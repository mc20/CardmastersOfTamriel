using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Processors;

public class ImageProcessingCoordinator
{
    private readonly Config _config;
    private readonly MasterMetadataHandler _metadataHandler;

    public ImageProcessingCoordinator(Config config, MasterMetadataHandler metadataHandler)
    {
        _config = config;
        _metadataHandler = metadataHandler;
    }

    public void BeginProcessing(ICardSetProcessor cardSetProcessor)
    {
        _metadataHandler.InitializeEmptyMetadata();
        FileOperations.EnsureDirectoryExists(_config.Paths.OutputFolderPath);

        var processor = new CardTierProcessor(_config, _metadataHandler);

        foreach (var tierSourceFolderPath in
                 Directory.EnumerateDirectories(_config.Paths.SourceImagesFolderPath))
        {
            Log.Information($"Tier Source Folder Path: '{tierSourceFolderPath}'");

            var tierDestinationFolderPath = Path.Combine(_config.Paths.OutputFolderPath ?? string.Empty,
                Path.GetFileName(tierSourceFolderPath));
            FileOperations.EnsureDirectoryExists(tierDestinationFolderPath);

            processor.ProcessTierFolder(tierSourceFolderPath, tierDestinationFolderPath, cardSetProcessor);
        }
    }
}