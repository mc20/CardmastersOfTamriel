using CardmastersOfTamriel.ImageProcessor.Factories;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Models;

public class RebuildMasterMetadataData
{
    private readonly string _setId;

    public HashSet<string> ImageFilePathsAtDestination;
    public HashSet<Card> CardsFromSource;
    public HashSet<string> ValidUniqueIdentifiersDeterminedFromSource;
    public HashSet<string?> UniqueIdentifiersAtDestination;
    public HashSet<string?> ValidIdentifiersAtDestination;

    private RebuildMasterMetadataData(string setId,
        HashSet<string> imageFilePathsAtDestination,
        HashSet<Card> cardsFromSource,
        HashSet<string> validUniqueIdentifiersDeterminedFromSource,
        HashSet<string?> uniqueIdentifiersAtDestination,
        HashSet<string?> validIdentifiersAtDestination)
    {
        _setId = setId;
        ImageFilePathsAtDestination = imageFilePathsAtDestination;
        CardsFromSource = cardsFromSource;
        ValidUniqueIdentifiersDeterminedFromSource = validUniqueIdentifiersDeterminedFromSource;
        UniqueIdentifiersAtDestination = uniqueIdentifiersAtDestination;
        ValidIdentifiersAtDestination = validIdentifiersAtDestination;
    }

    public static RebuildMasterMetadataData Load(CardSet set, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var imageFilePathsAtDestination = ImageFilePathUtility.GetImageFilePathsFromFolder(set.DestinationAbsoluteFolderPath, ["*.dds"]);
        var imageFilePathsAtSource = ImageFilePathUtility.GetImageFilePathsFromFolder(set.SourceAbsoluteFolderPath).OrderBy(Path.GetFileNameWithoutExtension).ToHashSet();
        var cardsFromSource = CardFactory.CreateCardsFromImagesAtFolderPath(set, imageFilePathsAtSource, true);
        var validUniqueIdentifiersDeterminedFromSource = cardsFromSource.Select(card => card.Id).ToHashSet();
        var uniqueIdentifiersAtDestination = imageFilePathsAtDestination.Select(Path.GetFileNameWithoutExtension).ToHashSet();
        var validIdentifiersAtDestination = uniqueIdentifiersAtDestination.Intersect(validUniqueIdentifiersDeterminedFromSource).ToHashSet();

        return new RebuildMasterMetadataData(
            set.Id,
            imageFilePathsAtDestination,
            cardsFromSource,
            validUniqueIdentifiersDeterminedFromSource,
            uniqueIdentifiersAtDestination,
            validIdentifiersAtDestination);
    }

    public void LogInformation()
    {
        Log.Information($"{_setId}\tFound {ImageFilePathsAtDestination.Count} DDS images at destination path");
        Log.Information($"{_setId}\tCreated {CardsFromSource.Count} cards from source images");
        Log.Information($"{_setId}\tFound {ValidUniqueIdentifiersDeterminedFromSource.Count} unique identifiers from source images");
        Log.Information($"{_setId}\tFound {UniqueIdentifiersAtDestination.Count} unique identifiers from destination images");
        Log.Information($"{_setId}\tFound {ValidIdentifiersAtDestination.Count} valid unique identifiers at destination");
    }
}