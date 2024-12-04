using CardmastersOfTamriel.ImageProcessor.Configuration;
using CardmastersOfTamriel.ImageProcessor.Factories;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Helpers;

public static class CardSetImageConversionHelper
{
    public static HashSet<Card> DetermineAndGetAllTrackedCards(DefaultValuesForCards defaults, CardSet set, HashSet<Card> cardsFromMetadataFile,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cardFactory = new CardFactory(defaults);
        var cardsCreatedFromSource = cardFactory.CreateInitialCardsFromSource(set);
        Log.Debug(
            $"[{set.Id}]\tCombining {cardsFromMetadataFile.Count} Cards from metadata file and {cardsCreatedFromSource.Count} Cards from source folder path.");

        var combinedCardList = CardHelper.CreateConsolidatedCardList(cardsCreatedFromSource, cardsFromMetadataFile);
        Log.Debug($"[{set.Id}]\tMerging lists resulted in {combinedCardList.Count} total Cards.");

        return combinedCardList;
    }

    public static async Task<HashSet<string>> GetImageFilePathsToConvertAsync(HashSet<Card> allCardsBeingTracked, int maximumNumberOfCardsToInclude,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cardSetId = allCardsBeingTracked.FirstOrDefault()?.SetId ?? string.Empty;

        try
        {
            var existingCardsAtDestination = allCardsBeingTracked
                .Where(card => !string.IsNullOrWhiteSpace(card.DestinationAbsoluteFilePath)).ToHashSet();

            Log.Debug($"[{cardSetId}]:\tExisting cards at destination: {existingCardsAtDestination.Count}");

            var sampleSize = maximumNumberOfCardsToInclude - existingCardsAtDestination.Count;

            Log.Debug($"[{cardSetId}]:\tSample size is: {sampleSize}");

            var eligibleFilePathsForConversion = allCardsBeingTracked
                .Where(card => string.IsNullOrWhiteSpace(card.DestinationAbsoluteFilePath))
                .Select(card => card.SourceAbsoluteFilePath)
                .Where(filePath => !string.IsNullOrWhiteSpace(filePath)).ToHashSet();

            Log.Debug($"[{cardSetId}]:\tEligible file path count is: {eligibleFilePathsForConversion.Count}");

            return eligibleFilePathsForConversion.SelectRandomImageFilePaths(sampleSize).Order().ToHashSet();
        }
        catch (Exception e)
        {
            Log.Error(e, $"[cardSetId]:\tAn error occurred.");
            throw;
        }
    }
}