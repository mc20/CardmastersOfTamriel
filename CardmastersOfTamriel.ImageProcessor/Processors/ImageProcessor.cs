using CardmastersOfTamriel.ImageProcessorConsole.Utilities;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessorConsole.Processors;

public class ImageProcessingCoordinator
{
    private readonly AppConfig _appConfig;
    private readonly MasterMetadataHandler _metadataHandler;

    public ImageProcessingCoordinator(AppConfig appConfig, MasterMetadataHandler metadataHandler)
    {
        _appConfig = appConfig;
        _metadataHandler = metadataHandler;
    }

    public void BeginProcessing()
    {
        _metadataHandler.InitializeEmptyMetadata();
        FileOperations.EnsureDirectoryExists(_appConfig.OutputFolderPath ?? string.Empty);

        var processor = new CardTierProcessor(_appConfig, _metadataHandler);

        foreach (var tierSourceFolderPath in
                 Directory.EnumerateDirectories(_appConfig.SourceImagesFolderPath ?? string.Empty))
        {
            Log.Information($"Tier Source Folder Path: '{tierSourceFolderPath}'");

            var tierDestinationFolderPath = Path.Combine(_appConfig.OutputFolderPath ?? string.Empty,
                Path.GetFileName(tierSourceFolderPath));
            FileOperations.EnsureDirectoryExists(tierDestinationFolderPath);

            processor.ProcessTierFolder(tierSourceFolderPath, tierDestinationFolderPath);
        }
    }
}