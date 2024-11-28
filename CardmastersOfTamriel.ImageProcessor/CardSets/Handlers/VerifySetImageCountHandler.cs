using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class VerifySetImageCountHandler : ICardSetHandler
{
    private readonly Config _config;

    public VerifySetImageCountHandler(Config config)
    {
        _config = config;
    }
    
    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken, CardOverrideData? overrideData = null)
    {
        var setmetadataFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForSetMetadataJson);
        if (File.Exists(setmetadataFilePath))
        {
            var setMetadata = JsonFileReader.ReadFromJsonAsync<CardSet>(setmetadataFilePath, cancellationToken);
            
            var imageAtSource = ImageFilePathUtility.GetImageFilePathsFromFolder(set.SourceAbsoluteFolderPath);
            var imagesAtDestination = ImageFilePathUtility.GetImageFilePathsFromFolder(set.DestinationAbsoluteFolderPath, ["*.dds"]);
            var maximumNumberOfCardsToInclude = ImageProcessingCoordinator.GetMaximumNumberOfCardsToInclude(set.Tier, imagesAtDestination.Count, _config);
            
            if (imagesAtDestination.Count > maximumNumberOfCardsToInclude)
            {
                Log.Warning($"{set.Id}:\tThere are {imagesAtDestination.Count} images at destination path, which exceeds the maximum number of cards required ({maximumNumberOfCardsToInclude}).");
            }
            
            if ((set.Tier == CardTier.Tier4) && (imagesAtDestination.Count < maximumNumberOfCardsToInclude))
            {
                Log.Warning($"{set.Id}:\tThere are {imagesAtDestination.Count} images at destination path, which is less than the expected number of cards ({maximumNumberOfCardsToInclude}).");
            }
        }
    }
}