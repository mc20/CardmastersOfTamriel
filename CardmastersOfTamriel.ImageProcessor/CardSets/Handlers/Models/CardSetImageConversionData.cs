using CardmastersOfTamriel.ImageProcessor.Factories;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Models;

public class CardSetImageConversionData
{
    public readonly HashSet<Card> CardsAtDestination;
    public readonly List<Card> FinalCards;

    private CardSetImageConversionData(HashSet<Card> cardsAtDestination, List<Card> finalCards)
    {
        CardsAtDestination = cardsAtDestination;
        FinalCards = finalCards;
    }

    public static CardSetImageConversionData Load(CardSet set, HashSet<Card> cardsFromMetadataFile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var imageFilePathsAtSource = ImageFilePathUtility.GetImageFilePathsFromFolder(set.SourceAbsoluteFolderPath).OrderBy(file => file).ToList();
        Log.Debug($"Found {imageFilePathsAtSource.Count} images at source path");

        var cardsFromSource = CardFactory.CreateCardsFromImagesAtFolderPath(set, [.. imageFilePathsAtSource], true);
        Log.Debug($"Created {cardsFromSource.Count} cards from source images");

        var updatedCards = cardsFromMetadataFile.ConsolidateCardsWith(cardsFromSource);
        Log.Debug($"Consolidated {updatedCards.Count} cards from metadata and source images");

        var imageFilePathsAtDestination = ImageFilePathUtility.GetImageFilePathsFromFolder(set.DestinationAbsoluteFolderPath, ["*.dds"]);
        Log.Debug($"Found {imageFilePathsAtDestination.Count} DDS images at destination path");

        var cardsAtDestination = CardFactory.CreateCardsFromImagesAtFolderPath(set, [.. imageFilePathsAtDestination], false);
        var finalCards = updatedCards.ConsolidateCardsWith(cardsAtDestination).ToList();

        return new CardSetImageConversionData(cardsAtDestination, finalCards);
    }
}