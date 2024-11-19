using CardmastersOfTamriel.ImageProcessor.CardSets;
using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Processors;

public static class ImageProcessingCoordinator
{
    public static async Task BeginProcessingAsync(ICardSetHandler asyncCardSetHandler, CancellationToken cancellationToken)
    {
        var config = ConfigurationProvider.Instance.Config;

        var handler = await MasterMetadataProvider.InstanceAsync(cancellationToken);
        await handler.MetadataHandler.LoadFromFileAsync(cancellationToken);
        await handler.MetadataHandler.CreateBackupAsync(cancellationToken);
        handler.MetadataHandler.InitializeEmptyMetadata();

        FileOperations.EnsureDirectoryExists(config.Paths.OutputFolderPath);

        foreach (var tierSourceFolderPath in
                 Directory.EnumerateDirectories(config.Paths.SourceImagesFolderPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            Log.Information($"Tier Source Folder Path: '{tierSourceFolderPath}'");

            var tierDestinationFolderPath = Path.Combine(config.Paths.OutputFolderPath ?? string.Empty,
                Path.GetFileName(tierSourceFolderPath));
            FileOperations.EnsureDirectoryExists(tierDestinationFolderPath);

            await CardTierProcessor.ProcessTierFolderAsync(tierSourceFolderPath, tierDestinationFolderPath, asyncCardSetHandler, cancellationToken);
        }
    }
}