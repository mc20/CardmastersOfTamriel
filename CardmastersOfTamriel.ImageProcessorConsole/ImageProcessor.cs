using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public class ImageProcessor
{
    private readonly AppConfig _appConfig;
    private readonly MasterMetadataHandler _metadataHandler;

    public ImageProcessor(AppConfig appConfig, MasterMetadataHandler metadataHandler)
    {
        _appConfig = appConfig;
        _metadataHandler = metadataHandler;
    }

    public void Start()
    {
        _metadataHandler.InitializeEmptyMetadata();
        FileOperations.EnsureDirectoryExists(_appConfig.OutputFolderPath ?? string.Empty);

        var processor = new CardTierProcessor(_appConfig, _metadataHandler);
        
        foreach (var tierSourceFolderPath in
                 Directory.EnumerateDirectories(_appConfig.SourceFolderPath ?? string.Empty))
        {
            Log.Information($"Tier Source Folder Path: '{tierSourceFolderPath}'");

            var tierDestinationFolderPath = Path.Combine(_appConfig.OutputFolderPath ?? string.Empty,
                Path.GetFileName(tierSourceFolderPath));
            FileOperations.EnsureDirectoryExists(tierDestinationFolderPath);

            processor.ProcessTierFolder(tierSourceFolderPath, tierDestinationFolderPath);
        }
    }
}