using CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Helpers;
using CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Models;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class ChangeNumberOfCardsInSetHandler : ICardSetHandler
{
    private readonly Config _config;

    public ChangeNumberOfCardsInSetHandler(Config config)
    {
        _config = config;
    }

    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken, CardSetBasicMetadata? setOverride = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var setMetadataFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForSetMetadataJson);
        if (File.Exists(setMetadataFilePath))
        {
            var cardsetMetadata = await JsonFileReader.ReadFromJsonAsync<CardSet>(setMetadataFilePath, cancellationToken);
            cardsetMetadata.Cards = []; // Clear the cards list and rebuild it

            var cardsJsonlFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForCardsJsonl);
            if (File.Exists(cardsJsonlFilePath))
            {
                try
                {
                    if (setOverride is not null)
                    {
                        Log.Information($"{set.Id}\t'{set.DisplayName}':\tRefreshing data from set override file");
                        cardsetMetadata.OverrideWith(setOverride);
                    }

                    var data = RebuildMasterMetadataData.Load(set, cancellationToken);

                    var cards = await JsonFileReader.LoadAllFromJsonLineFileAsync<Card>(cardsJsonlFilePath, cancellationToken);
                    var totalCardCountToDisplayOnCard = PruneCardsIfMaximumSampleSizeIsExceeded(_config, set, cards);

                    var formatter = new CardMetadataUpdater(this, set, data, _config, totalCardCountToDisplayOnCard);

                    var displayedIndex = 1;
                    var maxDisplayNameLength = 0;

                    foreach (var card in cards)
                    {
                        formatter.UpdateCardMetadataAndPublishHandlingProgress(card, ref displayedIndex, ref maxDisplayNameLength, cancellationToken, assignRelativePath: false);
                        cardsetMetadata.Cards.Add(card);
                    }

                    await JsonFileWriter.WriteToJsonLineFileAsync(cards, cardsJsonlFilePath, cancellationToken);
                    await JsonFileWriter.WriteToJsonAsync(cardsetMetadata, setMetadataFilePath, cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{set.Id}\tFailed to load cards from {cardsJsonlFilePath}");
                }
            }
        }
        else
        {
            Log.Error($"{set.Id}\tNo set metadata file found at {setMetadataFilePath}");
        }
    }

    private static uint PruneCardsIfMaximumSampleSizeIsExceeded(Config config, CardSet set, List<Card> cards)
    {
        int totalCardCountToDisplayOnCard;

        var cardsWithRelativePaths = cards.Where(c => !string.IsNullOrEmpty(c.DestinationRelativeFilePath)).ToHashSet();

        // Make sure we don't exceed the max sample size but respect unlimited card set sizes
        var maximumNumberOfCards =
            ImageProcessingCoordinator.GetMaximumNumberOfCardsToInclude(set.Tier, cards.Count, config);
        Log.Information($"{set.Id}\tChecking if the number of cards exceeds the maximum sample size of {maximumNumberOfCards}.. (current count: {cardsWithRelativePaths.Count})");

        if (cardsWithRelativePaths.Count > maximumNumberOfCards)
        {
            Log.Information($"{set.Id}\tPruning cards to {maximumNumberOfCards} from {cardsWithRelativePaths.Count}");

            var random = new Random();
            var newRandomCardSelections = cardsWithRelativePaths.OrderBy(c => random.Next()).Take(maximumNumberOfCards).ToHashSet();
            totalCardCountToDisplayOnCard = newRandomCardSelections.Count;

            foreach (var card in cards.Where(card => !newRandomCardSelections.Contains(card)))
            {
                card.DestinationRelativeFilePath = null;
            }
        }
        else
        {
            totalCardCountToDisplayOnCard = cardsWithRelativePaths.Count;

            if (cardsWithRelativePaths.Count < maximumNumberOfCards)
                Log.Warning($"{set.Id}\tThere aren't enough cards to meet the desired sample size of {maximumNumberOfCards}. Run convert handler again if additional images exist.");
        }

        return (uint)Math.Max(0, totalCardCountToDisplayOnCard);
    }
}