using CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Helpers;
using CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Models;
using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class CardSetImageConversionHandler : ICardSetHandler
{
    private readonly Config _config;

    public CardSetImageConversionHandler(Config config)
    {
        _config = config;
    }

    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken, CardSetBasicMetadata? setOverride = null)
    {
        Log.Information($"Processing Set from Source Path: '{set.SourceAbsoluteFolderPath}'");

        set.Cards ??= [];
        set.Cards.Clear();

        if (setOverride is not null)
        {
            Log.Information($"{set.Id}\t'{set.DisplayName}':\tRefreshing data from set override file");
            set.OverrideWith(setOverride);
        }

        var savedJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForCardsJsonl);
        var savedJsonBackupFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForCardsJsonlBackup);

        var cardsFromMetadataFile =
            await LoadCardsFromJsonFileAsync(savedJsonFilePath, savedJsonBackupFilePath, cancellationToken);

        var data = CardSetImageConversionData.Load(set, cardsFromMetadataFile, cancellationToken);
        Log.Debug($"Created {data.CardsAtDestination.Count} cards from destination images");
        Log.Debug($"Consolidated {data.FinalCards.Count} cards from metadata, source, and destination images");

        await ProcessEligibleImagesAsync(set, data.FinalCards, data.CardsAtDestination, cancellationToken);

        CardSetImageConversionHelper.UpdateDisplayCards(data.FinalCards);

        await JsonFileWriter.WriteToJsonLineFileAsync(data.FinalCards, savedJsonFilePath, cancellationToken);

        set.Cards = [.. data.FinalCards];
    }

    private static async Task<HashSet<Card>> LoadCardsFromJsonFileAsync(string savedJsonFilePath,
        string savedJsonBackupFilePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(savedJsonFilePath)) return [];

        if (File.Exists(savedJsonBackupFilePath))
        {
            File.Delete(savedJsonBackupFilePath);
        }

        File.Copy(savedJsonFilePath, savedJsonBackupFilePath);

        var cardsFromMetadataFile =
            await JsonFileReader.LoadAllFromJsonLineFileAsync<Card>(savedJsonFilePath, cancellationToken);

        File.Delete(savedJsonFilePath);

        return [.. cardsFromMetadataFile];
    }

    private async Task ProcessEligibleImagesAsync(CardSet set, List<Card> finalCards, HashSet<Card> cardsAtDestination,
        CancellationToken cancellationToken)
    {
        var eligibleFilePathsForConversion = finalCards.Select(card => card.SourceAbsoluteFilePath ?? string.Empty)
            .Where(filePath => !string.IsNullOrWhiteSpace(filePath)).ToHashSet();

        Log.Debug($"Found {eligibleFilePathsForConversion.Count} eligible images for conversion (no destination specified) from {finalCards.Count} cards");

        var maximumNumberOfCards = ImageProcessingCoordinator.GetMaximumNumberOfCardsToProcess(set.Tier, eligibleFilePathsForConversion.Count, _config);

        var needMoreRandomCards = cardsAtDestination.Count < maximumNumberOfCards;
        Log.Debug($"Maximum Number of Cards: {maximumNumberOfCards} while there are {cardsAtDestination.Count} cards at destination. Need more random cards? {needMoreRandomCards}");

        var randomCards = needMoreRandomCards
            ? ImageFilePathUtility.SelectRandomImageFilePaths(maximumNumberOfCards - cardsAtDestination.Count,
                eligibleFilePathsForConversion)
            : [];

        Log.Debug(needMoreRandomCards
            ? $"Selected {randomCards.Count} random images for conversion"
            : "No more random images needed for conversion");

        await ProcessAllCards(set, finalCards, randomCards, cancellationToken);
    }

    private async Task ProcessAllCards(CardSet set, List<Card> finalCards, HashSet<string> randomCards,
        CancellationToken cancellationToken)
    {
        foreach (var info in finalCards.OrderBy(card => card.Id).Select((card, index) => (card, index)))
        {
            if (!string.IsNullOrWhiteSpace(info.card.SourceAbsoluteFilePath) &&
                randomCards.Contains(info.card.SourceAbsoluteFilePath))
            {
                Log.Information($"Processing Card {Path.GetFileName(info.card.SourceAbsoluteFilePath)} for conversion");
                await CardSetImageConversionHelper.ProcessAndUpdateCardForConversion(_config, set, info.card, info.index,
                    finalCards.Count, cancellationToken);
            }
            else
            {
                if (string.IsNullOrEmpty(info.card.DestinationAbsoluteFilePath))
                {
                    Log.Verbose($"Card {info.card.Id} was not converted and will be skipped");
                    CardSetImageConversionHelper.UpdateUnconvertedCard(_config, info.card, (uint)info.index,
                        (uint)finalCards.Count);
                }
                else
                {
                    Log.Verbose($"Card {info.card.Id} was possibly already converted and will be used as-is");
                    info.card.DestinationRelativeFilePath =
                        FilePathHelper.GetRelativePath(info.card.DestinationAbsoluteFilePath, set.Tier);
                }
            }

            EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(info.card));
        }
    }
}