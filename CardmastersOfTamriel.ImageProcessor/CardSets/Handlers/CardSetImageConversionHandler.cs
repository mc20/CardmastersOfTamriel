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

    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken, CardOverrideData? overrideData = null)
    {
        try
        {
            Log.Information($"Processing Set from Source Path: '{set.SourceAbsoluteFolderPath}'");

            set.Cards ??= [];
            set.Cards.Clear();

            var cardsFromMetadataFile = new HashSet<Card>();

            var savedJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForCardsJsonl);
            if (File.Exists(savedJsonFilePath))
            {
                var cards = await JsonFileReader.LoadAllFromJsonLineFileAsync<Card>(savedJsonFilePath, cancellationToken);
                cardsFromMetadataFile = cards.ToHashSet();
            }

            BackupExistingCardJsonLineFile(set);

            var data = CardSetImageConversionData.Load(_config, set, cardsFromMetadataFile, cancellationToken);
            data.LogDataAsInformation();

            if (IsCardCountSufficient(set, data, overrideData))
            {
                Log.Information("Card count is sufficient, skipping image conversion");
                return;
            }

            await ProcessEligibleImagesAsync(set, data.ConsolidatedCardsFromDestinationAndSource, data.CardsOnlyAtDestination, cancellationToken);

            CardSetImageConversionHelper.UpdateDisplayedInformationOnCards(data.ConsolidatedCardsFromDestinationAndSource);

            await SaveCardSetDataAsync(set, data, savedJsonFilePath, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{set.Id}\tFailed to process Card set");
            throw;
        }
    }

    private static void BackupExistingCardJsonLineFile(CardSet set)
    {
        var savedJsonLineFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForCardsJsonl);
        var savedJsonLineBackupFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForCardsJsonlBackup);

        if (!File.Exists(savedJsonLineFilePath)) return;

        File.Move(savedJsonLineFilePath, savedJsonLineBackupFilePath, true);
        File.Delete(savedJsonLineFilePath);
        Log.Information("Backed up existing Card metadata file to '{SavedJsonLineBackupFilePath}'", savedJsonLineBackupFilePath);
    }

    private bool IsCardCountSufficient(CardSet set, CardSetImageConversionData data, CardOverrideData? overrideData)
    {
        var maximumNumberOfCardsToInclude =
            ImageProcessingCoordinator.GetMaximumNumberOfCardsToInclude(set.Tier, data.ConsolidatedCardsFromDestinationAndSource.Count, _config);
        var cardsAlreadyBeingIncludedForGame = data.ConsolidatedCardsFromDestinationAndSource.Count(c => !string.IsNullOrEmpty(c.DestinationRelativeFilePath));

        Log.Information($"{set.Id}:\tMaximum Number of Cards: {maximumNumberOfCardsToInclude}");
        Log.Information(
            $"{set.Id}:\tThere are {cardsAlreadyBeingIncludedForGame} Cards already being included for the game based on the Card DestinationRelativeFilePath value");

        if (cardsAlreadyBeingIncludedForGame < maximumNumberOfCardsToInclude) return false;

        Log.Information(
            $"{set.Id}:\tThere are {cardsAlreadyBeingIncludedForGame} Images already being included for the game, which meets or exceeds the maximum number of Cards required ({maximumNumberOfCardsToInclude}).");

        foreach (var card in data.ConsolidatedCardsFromDestinationAndSource)
        {
            if (overrideData is not null)
            {
                var isOverwritten = card.OverwriteWith(overrideData);
                if (isOverwritten) Log.Information("Overwrote card {CardId} with override data", card.Id);
            }

            EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(card));
        }

        return true;
    }

    private async Task ProcessEligibleImagesAsync(CardSet set,
        List<Card> consolidatedCardsFromDestinationAndSource,
        HashSet<Card> cardsAtDestination,
        CancellationToken cancellationToken)
    {
        var eligibleFilePathsForConversion = consolidatedCardsFromDestinationAndSource
            .Select(card => card.SourceAbsoluteFilePath)
            .Where(filePath => !string.IsNullOrWhiteSpace(filePath)).ToHashSet();

        Log.Debug(
            $"{set.Id}:\tFound {eligibleFilePathsForConversion.Count} eligible images for conversion (no destination specified) from {consolidatedCardsFromDestinationAndSource.Count} cards");

        var maximumNumberOfCards =
            ImageProcessingCoordinator.GetMaximumNumberOfCardsToInclude(set.Tier, consolidatedCardsFromDestinationAndSource.Count, _config);

        var needMoreRandomCards = cardsAtDestination.Count < maximumNumberOfCards;
        Log.Debug(
            $"{set.Id}:\tMaximum Number of Cards: {maximumNumberOfCards} while there are {cardsAtDestination.Count} cards at destination. Need more random cards? {needMoreRandomCards}");

        var randomCards = needMoreRandomCards
            ? ImageFilePathUtility.SelectRandomImageFilePaths(maximumNumberOfCards - cardsAtDestination.Count,
                eligibleFilePathsForConversion)
            : [];

        Log.Debug(needMoreRandomCards
            ? $"{set.Id}:\tSelected {randomCards.Count} random images for conversion"
            : $"{set.Id}:\tNo more random images needed for conversion");

        await ProcessAllCards(set, consolidatedCardsFromDestinationAndSource, randomCards, cancellationToken);
    }

    private async Task ProcessAllCards(CardSet set,
        List<Card> finalCards,
        HashSet<string> randomCards,
        CancellationToken cancellationToken)
    {
        foreach (var info in finalCards.OrderBy(card => card.Id).Select((card, index) => (card, index)))
        {
            if (!string.IsNullOrWhiteSpace(info.card.SourceAbsoluteFilePath) && randomCards.Contains(info.card.SourceAbsoluteFilePath))
            {
                Log.Information($"{set.Id}:\tProcessing Card {Path.GetFileName(info.card.SourceAbsoluteFilePath)} for conversion");
                await CardSetImageConversionHelper.ProcessAndUpdateCardForConversion(_config,
                    set,
                    info.card,
                    info.index,
                    finalCards.Count,
                    cancellationToken);
            }
            else
            {
                HandleUnconvertedCard(set, info, finalCards.Count);
            }

            EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(info.card));
        }
    }

    private void HandleUnconvertedCard(CardSet set, (Card card, int index) info, int totalCards)
    {
        if (string.IsNullOrEmpty(info.card.DestinationAbsoluteFilePath))
        {
            Log.Verbose($"{set.Id}:\tCard {info.card.Id} was not converted and will be skipped");
            CardSetImageConversionHelper.UpdateUnconvertedCard(_config, info.card, (uint)info.index, (uint)totalCards);
        }
        else
        {
            Log.Verbose($"{set.Id}:\tCard {info.card.Id} was possibly already converted and will be used as-is");
            info.card.DestinationRelativeFilePath = FilePathHelper.GetRelativePath(info.card.DestinationAbsoluteFilePath, set.Tier);
        }
    }

    private static async Task SaveCardSetDataAsync(CardSet set, CardSetImageConversionData data, string savedJsonFilePath, CancellationToken cancellationToken)
    {
        await JsonFileWriter.WriteToJsonLineFileAsync(data.ConsolidatedCardsFromDestinationAndSource, savedJsonFilePath, cancellationToken);
        set.Cards = [.. data.ConsolidatedCardsFromDestinationAndSource];
        await JsonFileWriter.WriteToJsonAsync(set, Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForSetMetadataJson),
            cancellationToken);
    }
}