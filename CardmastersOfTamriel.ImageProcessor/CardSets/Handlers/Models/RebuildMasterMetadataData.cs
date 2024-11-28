using CardmastersOfTamriel.ImageProcessor.Factories;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Models;

public class RebuildMasterMetadataData
{
    private readonly string _setId;

    public readonly HashSet<string> ImageFilePathsAtDestination;
    public readonly HashSet<Card> CardsFromSource;
    public readonly HashSet<string> ValidUniqueIdentifiersDeterminedFromSource;
    private readonly HashSet<string?> _uniqueIdentifiersAtDestination;
    public readonly HashSet<string?> ValidIdentifiersAtDestination;

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
        _uniqueIdentifiersAtDestination = uniqueIdentifiersAtDestination;
        ValidIdentifiersAtDestination = validIdentifiersAtDestination;
    }

    public static RebuildMasterMetadataData Load(Config config, CardSet set, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var cardFactory = new CardFactory(config);

        var imageFilePathsAtDestination = ImageFilePathUtility.GetImageFilePathsFromFolder(set.DestinationAbsoluteFolderPath, ["*.dds"]);
        var imageFilePathsAtSource = ImageFilePathUtility.GetImageFilePathsFromFolder(set.SourceAbsoluteFolderPath).OrderBy(Path.GetFileNameWithoutExtension).ToHashSet();
        var cardsFromSource = cardFactory.CreateCardsFromImagesAtFolderPath(set, imageFilePathsAtSource);
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

    public void LogDataAsInformation()
    {
        Log.Information($"{_setId}\tFound {ImageFilePathsAtDestination.Count} DDS Images at destination path");
        Log.Information($"{_setId}\tCreated {CardsFromSource.Count} Cards based on source Images");
        Log.Information($"{_setId}\tFound {ValidUniqueIdentifiersDeterminedFromSource.Count} unique identifiers from source Images");
        Log.Information($"{_setId}\tFound {_uniqueIdentifiersAtDestination.Count} unique identifiers from destination Images");
        Log.Information($"{_setId}\tFound {ValidIdentifiersAtDestination.Count} valid unique identifiers at destination");
    }
}