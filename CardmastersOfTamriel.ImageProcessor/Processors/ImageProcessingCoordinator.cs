using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Processors;

public static class ImageProcessingCoordinator
{
    public static void BeginProcessing(ICardSetHandler cardSetProcessor)
    {
        var config = ConfigurationProvider.Instance.Config;

        var handler = MasterMetadataProvider.Instance.MetadataHandler;

        handler.LoadFromFile();
        handler.CreateBackup();
        handler.InitializeEmptyMetadata();

        FileOperations.EnsureDirectoryExists(config.Paths.OutputFolderPath);

        foreach (var tierSourceFolderPath in
                 Directory.EnumerateDirectories(config.Paths.SourceImagesFolderPath))
        {
            Log.Information($"Tier Source Folder Path: '{tierSourceFolderPath}'");

            var tierDestinationFolderPath = Path.Combine(config.Paths.OutputFolderPath ?? string.Empty,
                Path.GetFileName(tierSourceFolderPath));
            FileOperations.EnsureDirectoryExists(tierDestinationFolderPath);

            CardTierProcessor.ProcessTierFolder(tierSourceFolderPath, tierDestinationFolderPath, cardSetProcessor);
        }
    }
}