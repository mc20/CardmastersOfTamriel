using CardmastersOfTamriel.ImageProcessor.Configuration;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Tasks;

public static class CleanupNonTrackedFilesAtDestinationTask
{
    public static async Task CleanupNonTrackedFiles(PathSettings pathSettings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            foreach (var seriesFolder in Directory.EnumerateDirectories(pathSettings.OutputFolderPath, "*", SearchOption.AllDirectories))
            foreach (var setFolder in Directory.EnumerateDirectories(seriesFolder, "*", SearchOption.TopDirectoryOnly))
            {
                var setMetadataFile = Path.Combine(setFolder, PathSettings.DefaultFilenameForSetMetadataJson);
                if (!File.Exists(setMetadataFile)) continue;

                var cardSet = await JsonFileReader.ReadFromJsonAsync<CardSet>(setMetadataFile, cancellationToken);
                if (cardSet.Cards is null) continue;

                var destinations = cardSet.Cards.Select(card => card.DestinationAbsoluteFilePath).ToHashSet();
                var imagePathsInFolder = ImageFilePathUtility.GetImageFilePathsFromFolder(setFolder, ["*.png", "*.jpg", "*.jpeg", "*.dds"]);
                var nonTrackedFiles = imagePathsInFolder.Except(destinations).ToHashSet();

                foreach (var nonTrackedFile in nonTrackedFiles)
                {
                    if (string.IsNullOrWhiteSpace(nonTrackedFile)) continue;

                    Log.Information("Deleting non-tracked file: {nonTrackedFile}", nonTrackedFile);
                    File.Delete(nonTrackedFile);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "An error occurred while cleaning up non-tracked files at destination");
        }
    }
}