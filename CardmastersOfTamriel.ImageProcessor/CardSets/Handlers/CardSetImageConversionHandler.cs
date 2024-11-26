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
        Log.Debug($"{set.Id}:\tCreated {data.CardsOnlyAtDestination.Count} cards from destination images");
        Log.Debug($"{set.Id}:\tConsolidated {data.ConsolidatedCardsFromDestinationAndSource.Count} cards from metadata, source, and destination images");
        
        var maximumNumberOfCardsToInclude = ImageProcessingCoordinator.GetMaximumNumberOfCardsToInclude(set.Tier, data.ConsolidatedCardsFromDestinationAndSource.Count, _config);
        Log.Debug($"{set.Id}:\tMaximum Number of Cards: {maximumNumberOfCardsToInclude}");
        var imagesAlreadyBeingIncludedForGame = data.ConsolidatedCardsFromDestinationAndSource.Count(c => !string.IsNullOrEmpty(c.DestinationRelativeFilePath));
        Log.Debug($"{set.Id}:\tThere are {imagesAlreadyBeingIncludedForGame} images already being included for the game");

        if (imagesAlreadyBeingIncludedForGame >= maximumNumberOfCardsToInclude)
        {
            Log.Information($"{set.Id}:\tThere are {imagesAlreadyBeingIncludedForGame} images already being included for the game, which meets or exceeds the maximum number of cards required ({maximumNumberOfCardsToInclude}).");

            foreach (var card in data.ConsolidatedCardsFromDestinationAndSource)
            {
                EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(card));   
            }
            
            return;
        }

        await ProcessEligibleImagesAsync(set, data.ConsolidatedCardsFromDestinationAndSource, data.CardsOnlyAtDestination, cancellationToken);

        CardSetImageConversionHelper.UpdateDisplayCards(data.ConsolidatedCardsFromDestinationAndSource);

        await JsonFileWriter.WriteToJsonLineFileAsync(data.ConsolidatedCardsFromDestinationAndSource, savedJsonFilePath, cancellationToken);

        set.Cards = [.. data.ConsolidatedCardsFromDestinationAndSource];
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

    private async Task ProcessEligibleImagesAsync(CardSet set, List<Card> consolidatedCardsFromDestinationAndSource, HashSet<Card> cardsAtDestination,
        CancellationToken cancellationToken)
    {
        var eligibleFilePathsForConversion = consolidatedCardsFromDestinationAndSource.Select(card => card.SourceAbsoluteFilePath ?? string.Empty)
            .Where(filePath => !string.IsNullOrWhiteSpace(filePath)).ToHashSet();

        Log.Debug($"{set.Id}:\tFound {eligibleFilePathsForConversion.Count} eligible images for conversion (no destination specified) from {consolidatedCardsFromDestinationAndSource.Count} cards");

        var maximumNumberOfCards = ImageProcessingCoordinator.GetMaximumNumberOfCardsToInclude(set.Tier, consolidatedCardsFromDestinationAndSource.Count, _config);

        var needMoreRandomCards = cardsAtDestination.Count < maximumNumberOfCards;
        Log.Debug($"{set.Id}:\tMaximum Number of Cards: {maximumNumberOfCards} while there are {cardsAtDestination.Count} cards at destination. Need more random cards? {needMoreRandomCards}");

        var randomCards = needMoreRandomCards
            ? ImageFilePathUtility.SelectRandomImageFilePaths(maximumNumberOfCards - cardsAtDestination.Count,
                eligibleFilePathsForConversion)
            : [];

        Log.Debug(needMoreRandomCards
            ? $"{set.Id}:\tSelected {randomCards.Count} random images for conversion"
            : $"{set.Id}:\tNo more random images needed for conversion");

        await ProcessAllCards(set, consolidatedCardsFromDestinationAndSource, randomCards, cancellationToken);
    }

    private async Task ProcessAllCards(CardSet set, List<Card> finalCards, HashSet<string> randomCards,
        CancellationToken cancellationToken)
    {
        foreach (var info in finalCards.OrderBy(card => card.Id).Select((card, index) => (card, index)))
        {
            if (!string.IsNullOrWhiteSpace(info.card.SourceAbsoluteFilePath) &&
                randomCards.Contains(info.card.SourceAbsoluteFilePath))
            {
                Log.Information($"{set.Id}:\tProcessing Card {Path.GetFileName(info.card.SourceAbsoluteFilePath)} for conversion");
                await CardSetImageConversionHelper.ProcessAndUpdateCardForConversion(_config, set, info.card, info.index,
                    finalCards.Count, cancellationToken);
            }
            else
            {
                if (string.IsNullOrEmpty(info.card.DestinationAbsoluteFilePath))
                {
                    Log.Verbose($"{set.Id}:\tCard {info.card.Id} was not converted and will be skipped");
                    CardSetImageConversionHelper.UpdateUnconvertedCard(_config, info.card, (uint)info.index,
                        (uint)finalCards.Count);
                }
                else
                {
                    Log.Verbose($"{set.Id}:\tCard {info.card.Id} was possibly already converted and will be used as-is");
                    info.card.DestinationRelativeFilePath =
                        FilePathHelper.GetRelativePath(info.card.DestinationAbsoluteFilePath, set.Tier);
                }
            }

            EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(info.card));
        }
    }
}