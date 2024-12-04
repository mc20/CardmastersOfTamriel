using CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Helpers;
using CardmastersOfTamriel.ImageProcessor.Configuration;
using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class CardSetImageConversionHandler(Config config) : ICardSetHandler
{
    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken, CardSetHandlerOverrideData? overrideData = null)
    {
        try
        {
            Log.Debug($"[{set.Id}]\tProcessing Set from Source Path: '{set.SourceAbsoluteFolderPath}'");

            set.Cards ??= [];
            set.Cards.Clear();

            var cardSetFromMetadataFile = await JsonFileReader.ReadFromJsonAsync<CardSet>(Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings
                .DefaultFilenameForSetMetadataJson), cancellationToken);
            cardSetFromMetadataFile.Cards ??= [];

            SetMetadataJsonFileHelper.BackupExistingSetMetadataFile(set);

            await CardSetCleanupHelper.RemoveCardsWithNoSourceAbsoluteFilePathAsync(cardSetFromMetadataFile, cancellationToken);

            await CardSetCleanupHelper.RemoveCardsWithNoExistingFileAtDestination(cardSetFromMetadataFile, cancellationToken);

            var allCardsBeingTracked =
                CardSetImageConversionHelper.DetermineAndGetAllTrackedCards(config.DefaultCardValues, set, cardSetFromMetadataFile.Cards, cancellationToken);

            Log.Debug($"[{set.Id}]\tTotal number of cards being tracked: {allCardsBeingTracked.Count}");

            var maximumNumberOfCardsToInclude =
                ImageProcessingCoordinator.GetMaximumNumberOfCardsToInclude(allCardsBeingTracked.Count, config.General, overrideData);
            Log.Debug($"[{set.Id}]\tMaximum number of cards to include: {maximumNumberOfCardsToInclude}");

            if (IsDestinationCardCountSufficient(allCardsBeingTracked, maximumNumberOfCardsToInclude))
            {
                HandleCardsWhenSufficient(set.Id, allCardsBeingTracked, overrideData);
            }
            else
            {
                var imageFilePathsFromSourceToConvert =
                    await CardSetImageConversionHelper.GetImageFilePathsToConvertAsync(allCardsBeingTracked, maximumNumberOfCardsToInclude, cancellationToken);

                Log.Debug("ImageFilePathsFromSourceToConvert: {ImageFilePathsFromSourceToConvert}", string.Join(",", imageFilePathsFromSourceToConvert));

                await ProcessAllCards(set, allCardsBeingTracked, imageFilePathsFromSourceToConvert, cancellationToken);
            }

            UpdateDisplayedInformationOnCards(allCardsBeingTracked, overrideData);

            set.Cards = [.. allCardsBeingTracked];
            await JsonFileWriter.WriteToJsonAsync(set, Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForSetMetadataJson),
                cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"[{set.Id}]\tFailed to process Card set");
            throw;
        }
    }

    private static bool IsDestinationCardCountSufficient(HashSet<Card> allTrackedCards, int maximumNumberOfCardsToInclude)
    {
        var firstCardSetId = allTrackedCards.FirstOrDefault()?.SetId ?? "";
        var cardsAlreadyBeingIncludedForGame = allTrackedCards.Count(c => !string.IsNullOrEmpty(c.DestinationRelativeFilePath));
        Log.Debug(
            $"[{firstCardSetId}]:\tCards already being included for game: {cardsAlreadyBeingIncludedForGame} out of {maximumNumberOfCardsToInclude} maximum allowed");
        return cardsAlreadyBeingIncludedForGame >= maximumNumberOfCardsToInclude;
    }

    private void HandleCardsWhenSufficient(string setId, HashSet<Card> allCardsBeingTracked, CardSetHandlerOverrideData? overrideData)
    {
        try
        {
            Log.Debug($"[{setId}]\tCard count is sufficient, overriding existing Card data and skipping image conversion");

            foreach (var card in allCardsBeingTracked)
            {
                if (overrideData is not null)
                {
                    var isOverwritten = card.OverwriteWith(overrideData);
                    if (isOverwritten) Log.Debug($"[{setId}]\tOverwrote card {{CardId}} with override data", card.Id);
                }

                EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(card));
            }
        }
        catch (Exception e)
        {
            Log.Error(e, $"[{setId}]\tError handling cards when card count is sufficient");
            throw;
        }
    }

    private async Task ProcessAllCards(CardSet set, HashSet<Card> allCardsBeingTracked, HashSet<string> imageFilePathsToConvert,
        CancellationToken cancellationToken)
    {
        Log.Debug($"[{set.Id}]\tProcessing {imageFilePathsToConvert.Count} cards for conversion...");


        try
        {
            var cardCount = 0;

            foreach (var info in allCardsBeingTracked.OrderBy(card => card.Id).Select((card, index) => (card, index)))
            {
                Log.Debug("Processing card {CardId} in set {SetId}", info.card.Id, set.Id);

                var conversionProcessor = new CardConversionProcessor(config.ImageSettings, info.card, (uint)info.index, (uint)allCardsBeingTracked.Count);
                if (imageFilePathsToConvert.Contains(info.card.SourceAbsoluteFilePath))
                {
                    await conversionProcessor.ProcessAndUpdateCardForConversionAsync(set, cancellationToken);
                    cardCount++;
                }
                else
                {
                    conversionProcessor.HandleUnconvertedCard(set);
                }

                EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(info.card));
            }

            Log.Information($"[{set.Id}]\tImage file paths to convert was {imageFilePathsToConvert.Count} and card count ended up being {cardCount}");
        }
        catch (Exception e)
        {
            Log.Error(e, $"[{set.Id}]\tError processing cards for conversion");
            throw;
        }
    }

    private static void UpdateDisplayedInformationOnCards(HashSet<Card> cards, CardSetHandlerOverrideData? overrideData)
    {
        Log.Debug("Updating displayed information on cards...");

        try
        {
            var cardsEligibleForDisplay = cards.Where(card => !string.IsNullOrWhiteSpace(card.DestinationRelativeFilePath)).ToList();
            foreach (var cardInfo in cardsEligibleForDisplay
                         .OrderBy(card => card.Id)
                         .Select((card, index) => (card, index)))
            {
                cardInfo.card.DisplayedIndex = (uint)cardInfo.index + 1;
                cardInfo.card.DisplayedTotalCount = (uint)cardsEligibleForDisplay.Count;
                cardInfo.card.SetGenericDisplayName();

                if (overrideData is null) continue;

                var isOverwritten = cardInfo.card.OverwriteWith(overrideData);
                if (isOverwritten) Log.Information($"[{overrideData.CardSetId}]\tOverwrote card {{CardId}} with override data", cardInfo.card.Id);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error updating displayed information on cards");
            throw;
        }
    }
}