using CardmastersOfTamriel.ImageProcessor.Factories;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Models;

public class CardSetImageConversionData
{
    private readonly string _setId;
    public readonly HashSet<Card> CardsOnlyAtDestination;
    public readonly List<Card> ConsolidatedCardsFromDestinationAndSource;

    private CardSetImageConversionData(HashSet<Card> cardsOnlyAtDestination, List<Card> consolidatedCardsFromDestinationAndSource, string setId)
    {
        CardsOnlyAtDestination = cardsOnlyAtDestination;
        ConsolidatedCardsFromDestinationAndSource = consolidatedCardsFromDestinationAndSource;
        _setId = setId;
    }

    public static CardSetImageConversionData Load(Config config, CardSet set, HashSet<Card> cardsFromMetadataFile,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cardFactory = new CardFactory(config);

        var imageFilePathsAtSource = ImageFilePathUtility.GetImageFilePathsFromFolder(set.SourceAbsoluteFolderPath).OrderBy(file => file).ToList();
        Log.Debug($"Found {imageFilePathsAtSource.Count} images at source path");

        var cardsFromSource = cardFactory.CreateCardsFromImagesAtFolderPath(set, [.. imageFilePathsAtSource]);
        Log.Debug($"Created {cardsFromSource.Count} cards from source images");

        var updatedCards = cardsFromMetadataFile.ConsolidateCardsWith(cardsFromSource);
        Log.Debug($"Consolidated {updatedCards.Count} cards from metadata and source images");

        var imageFilePathsAtDestination = ImageFilePathUtility.GetImageFilePathsFromFolder(set.DestinationAbsoluteFolderPath, ["*.dds"]);
        Log.Debug($"Found {imageFilePathsAtDestination.Count} DDS images at destination path");

        var cardsAtDestination = cardFactory.CreateCardsFromImagesAtFolderPath(set, [.. imageFilePathsAtDestination]);
        var finalCards = updatedCards.ConsolidateCardsWith(cardsAtDestination).ToList();

        return new CardSetImageConversionData(cardsAtDestination, finalCards, set.Id);
    }

    public void LogDataAsInformation()
    {
        Log.Information($"{_setId}:\tCreated {CardsOnlyAtDestination.Count} Cards from destination images");
        Log.Information($"{_setId}:\tConsolidated {ConsolidatedCardsFromDestinationAndSource.Count} Cards from metadata, source, and destination Images");
    }
}