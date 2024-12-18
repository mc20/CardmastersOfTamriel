using CardmastersOfTamriel.ImageProcessor.Configuration;
using CardmastersOfTamriel.ImageProcessor.Factories;
using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Setup;

public class CardSetReplicator(CardSeries series)
{
    public async Task HandleDestinationSetCreationAsync(Dictionary<string, List<string>> groupedFolders,
        CancellationToken cancellationToken)
    {
        foreach (var (setFolderName, sourceSetPaths) in groupedFolders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Log.Information($"Checking Set {setFolderName} having {sourceSetPaths.Count} source set folders");

            if (sourceSetPaths.Count > 1)
            {
                Log.Debug($"Creating multiple set folders for {setFolderName}");
                await CreateMultipleFoldersAsync(setFolderName, sourceSetPaths, cancellationToken);
            }
            else
            {
                Log.Debug($"Creating single set folder for {setFolderName}");
                var destinationSetFolderPath = Path.Combine(series.DestinationFolderPath, setFolderName);
                await SaveNewSetAndCreateAtDestinationAsync(setFolderName, destinationSetFolderPath, sourceSetPaths[0],
                    cancellationToken);
            }
        }
    }

    private async Task CreateMultipleFoldersAsync(string uniqueSetFolderName, List<string> sourceSetFolderPaths,
        CancellationToken cancellationToken)
    {
        // Multiple folders: rename with incremented suffixes
        for (var index = 0; index < sourceSetFolderPaths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var destinationSetFolderName = $"{uniqueSetFolderName}_{index + 1:D2}";
            var destinationSetFolderPath = Path.Combine(series.DestinationFolderPath, destinationSetFolderName);

            await SaveNewSetAndCreateAtDestinationAsync(destinationSetFolderName, destinationSetFolderPath,
                sourceSetFolderPaths[index], cancellationToken);
        }
    }

    private async Task SaveNewSetAndCreateAtDestinationAsync(string setFolderName, string destinationSetFolderPath,
        string sourceSetPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationSetFolderPath);
        Log.Debug("Created destination Set folder: " + destinationSetFolderPath);

        CardSet? newCardSetMetadata = null;

        var destinationSetMetadataFilePath = Path.Combine(destinationSetFolderPath, PathSettings.DefaultFilenameForSetMetadataJson);

        if (!File.Exists(destinationSetMetadataFilePath))
            Log.Debug($"No existing Series Metadata file found at Destination Path: {destinationSetMetadataFilePath}");
        else
            try
            {
                newCardSetMetadata = await JsonFileReader.ReadFromJsonAsync<CardSet>(destinationSetMetadataFilePath, cancellationToken);
                Log.Debug($"Found existing Series Metadata file at Destination Path: '{destinationSetMetadataFilePath}'");
            }
            catch (Exception e)
            {
                Log.Error(e, $"Could not convert the the Destination Metadata file at '{destinationSetMetadataFilePath}' to a CardSet");
            }

        if (newCardSetMetadata is null)
        {
            var newSetId = $"{setFolderName}-{Guid.NewGuid()}";
            newCardSetMetadata = CardSetFactory.CreateNewSet(newSetId, setFolderName, series);
            newCardSetMetadata.Tier = series.Tier;
        }

        // Ensure there's a properly formatted Set DisplayName and series keyword
        newCardSetMetadata.DisplayName = NamingHelper.FormatDisplayNameFromFolderName(setFolderName);
        newCardSetMetadata.SeriesKeyword = NamingHelper.CreateKeyword(series);
        newCardSetMetadata.SourceAbsoluteFolderPath = sourceSetPath;
        newCardSetMetadata.DestinationRelativeFolderPath = FilePathHelper.GetRelativePath(destinationSetFolderPath, newCardSetMetadata.Tier);
        newCardSetMetadata.DestinationAbsoluteFolderPath = destinationSetFolderPath;

        series.Sets ??= [];
        series.Sets.RemoveWhere(set => set.Id == newCardSetMetadata.Id);
        series.Sets.Add(newCardSetMetadata);

        await JsonFileWriter.WriteToJsonAsync(newCardSetMetadata, destinationSetMetadataFilePath, cancellationToken);

        EventBroker.PublishFolderPreparationProgress(this, new ProgressTrackingEventArgs(newCardSetMetadata));

        Log.Debug($"New serialized Card Set metadata written to {destinationSetMetadataFilePath}");
        Log.Debug($"New Set: '{newCardSetMetadata.Id}' saved to path: '{newCardSetMetadata.DestinationAbsoluteFolderPath}'");
    }
}