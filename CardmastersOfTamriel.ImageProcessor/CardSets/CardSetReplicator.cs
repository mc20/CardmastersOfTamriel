using System.Text.Json;
using CardmastersOfTamriel.ImageProcessor.Factories;
using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets;

public class CardSetReplicator
{
    private readonly MasterMetadataHandler _handler = MasterMetadataProvider.Instance.MetadataHandler;
    private readonly CardSeries _series;

    [Obsolete("Use CreateAsync instead", false)]
    public CardSetReplicator(string seriesId)
    {
        _series = _handler.Metadata.Series?.FirstOrDefault(series => series.Id == seriesId) ??
                  throw new KeyNotFoundException($"No series found with id: {seriesId}");
    }

    private CardSetReplicator(MasterMetadataHandler handler, string seriesId)
    {
        _handler = handler;
        _series = _handler.Metadata.Series?.FirstOrDefault(series => series.Id == seriesId) ??
                throw new KeyNotFoundException($"No series found with id: {seriesId}");
    }

    public static async Task<CardSetReplicator> CreateAsync(string seriesId, CancellationToken cancellationToken)
    {
        var provider = await MasterMetadataProvider.InstanceAsync(cancellationToken);
        return new CardSetReplicator(provider.MetadataHandler, seriesId);
    }

    [Obsolete("Use HandleDestinationSetCreationAsync instead", false)]
    public void HandleDestinationSetCreation(Dictionary<string, List<string>> groupedFolders)
    {
        foreach (var (setFolderName, sourceSetPaths) in groupedFolders)
        {
            Log.Verbose($"Checking Set {setFolderName} having {sourceSetPaths.Count} source set folders");

            if (sourceSetPaths.Count > 1)
            {
                Log.Verbose($"Creating multiple set folders for {setFolderName}");
                CreateMultipleFolders(setFolderName, sourceSetPaths);
            }
            else
            {
                Log.Verbose($"Creating single set folder for {setFolderName}");
                var destinationSetFolderPath = Path.Combine(_series.DestinationFolderPath, setFolderName);
                SaveNewSetAndCreateAtDestination(setFolderName, destinationSetFolderPath, sourceSetPaths[0]);
            }
        }
    }

    public async Task HandleDestinationSetCreationAsync(Dictionary<string, List<string>> groupedFolders, CancellationToken cancellationToken)
    {
        foreach (var (setFolderName, sourceSetPaths) in groupedFolders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Log.Information($"Checking Set {setFolderName} having {sourceSetPaths.Count} source set folders");

            if (sourceSetPaths.Count > 1)
            {
                Log.Verbose($"Creating multiple set folders for {setFolderName}");
                await CreateMultipleFoldersAsync(setFolderName, sourceSetPaths, cancellationToken);
            }
            else
            {
                Log.Verbose($"Creating single set folder for {setFolderName}");
                var destinationSetFolderPath = Path.Combine(_series.DestinationFolderPath, setFolderName);
                await SaveNewSetAndCreateAtDestinationAsync(setFolderName, destinationSetFolderPath, sourceSetPaths[0], cancellationToken);
            }
        }
    }

    [Obsolete("Use CreateMultipleFoldersAsync instead", false)]
    private void CreateMultipleFolders(string uniqueSetFolderName, List<string> sourceSetFolderPaths)
    {
        // Multiple folders: rename with incremented suffixes
        for (var index = 0; index < sourceSetFolderPaths.Count; index++)
        {
            var destinationSetFolderName = $"{uniqueSetFolderName}_{index + 1:D2}";
            var destinationSetFolderPath = Path.Combine(_series.DestinationFolderPath, destinationSetFolderName);

            SaveNewSetAndCreateAtDestination(destinationSetFolderName, destinationSetFolderPath,
                sourceSetFolderPaths[index]);
        }
    }

    private async Task CreateMultipleFoldersAsync(string uniqueSetFolderName, List<string> sourceSetFolderPaths, CancellationToken cancellationToken)
    {
        // Multiple folders: rename with incremented suffixes
        for (var index = 0; index < sourceSetFolderPaths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var destinationSetFolderName = $"{uniqueSetFolderName}_{index + 1:D2}";
            var destinationSetFolderPath = Path.Combine(_series.DestinationFolderPath, destinationSetFolderName);

            await SaveNewSetAndCreateAtDestinationAsync(destinationSetFolderName, destinationSetFolderPath,
                sourceSetFolderPaths[index], cancellationToken);
        }
    }

    [Obsolete("Use SaveNewSetAndCreateAtDestinationAsync instead", false)]
    private void SaveNewSetAndCreateAtDestination(string setFolderName, string destinationSetFolderPath,
            string sourceSetPath)
    {
        Directory.CreateDirectory(destinationSetFolderPath);
        Log.Verbose("Created destination Set folder: " + destinationSetFolderPath);

        CardSet? newCardSetMetadata = null;

        var destinationSetMetadataFilePath = Path.Combine(destinationSetFolderPath, "set_metadata.json");

        if (!File.Exists(destinationSetMetadataFilePath))
        {
            Log.Verbose("No existing Series Metadata file found at Destination Path: " +
                        destinationSetMetadataFilePath);
            _handler.WriteMetadataToFile();
        }
        else
        {
            try
            {
                newCardSetMetadata = JsonFileReader.ReadFromJson<CardSet?>(destinationSetMetadataFilePath);
                Log.Verbose(
                    $"Found existing Series Metadata file at Destination Path: '{destinationSetMetadataFilePath}'");
            }
            catch (Exception e)
            {
                Log.Error(e,
                    $"Could not convert the the Destination Metadata file at '{destinationSetMetadataFilePath}' to a CardSet");
            }
        }

        if (newCardSetMetadata is null)
        {
            newCardSetMetadata = CardSetFactory.CreateNewSet(setFolderName, _series);
            newCardSetMetadata.Tier = _series.Tier;
        }

        // Ensure there's a properly formatted Set DisplayName
        newCardSetMetadata.DisplayName = NameHelper.FormatDisplayNameFromId(newCardSetMetadata.Id);
        newCardSetMetadata.SourceAbsoluteFolderPath = sourceSetPath;
        newCardSetMetadata.DestinationAbsoluteFolderPath = destinationSetFolderPath;

        _series.Sets ??= [];
        _series.Sets.RemoveWhere(set => set.Id == newCardSetMetadata.Id);
        _series.Sets.Add(newCardSetMetadata);

        var serializedJson = JsonSerializer.Serialize(newCardSetMetadata, JsonSettings.Options);
        File.WriteAllText(destinationSetMetadataFilePath, serializedJson);
        Log.Information($"New serialized Card Set metadata written to {destinationSetMetadataFilePath}");

        Log.Information(
            $"New Set: '{newCardSetMetadata.Id}' saved to path: '{newCardSetMetadata.DestinationAbsoluteFolderPath}'");

        _handler.WriteMetadataToFile();
    }

    private async Task SaveNewSetAndCreateAtDestinationAsync(string setFolderName, string destinationSetFolderPath,
        string sourceSetPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationSetFolderPath);
        Log.Verbose("Created destination Set folder: " + destinationSetFolderPath);

        CardSet? newCardSetMetadata = null;

        var destinationSetMetadataFilePath = Path.Combine(destinationSetFolderPath, "set_metadata.json");

        if (!File.Exists(destinationSetMetadataFilePath))
        {
            Log.Verbose("No existing Series Metadata file found at Destination Path: " +
                        destinationSetMetadataFilePath);
            await _handler.WriteMetadataToFileAsync(cancellationToken);
        }
        else
        {
            try
            {
                newCardSetMetadata = await JsonFileReader.ReadFromJsonAsync<CardSet?>(destinationSetMetadataFilePath, cancellationToken);
                Log.Verbose(
                    $"Found existing Series Metadata file at Destination Path: '{destinationSetMetadataFilePath}'");
            }
            catch (Exception e)
            {
                Log.Error(e,
                    $"Could not convert the the Destination Metadata file at '{destinationSetMetadataFilePath}' to a CardSet");
            }
        }

        if (newCardSetMetadata is null)
        {
            newCardSetMetadata = CardSetFactory.CreateNewSet(setFolderName, _series);
            newCardSetMetadata.Tier = _series.Tier;
        }

        // Ensure there's a properly formatted Set DisplayName
        newCardSetMetadata.DisplayName = NameHelper.FormatDisplayNameFromId(newCardSetMetadata.Id);
        newCardSetMetadata.SourceAbsoluteFolderPath = sourceSetPath;
        newCardSetMetadata.DestinationAbsoluteFolderPath = destinationSetFolderPath;

        _series.Sets ??= [];
        _series.Sets.RemoveWhere(set => set.Id == newCardSetMetadata.Id);
        _series.Sets.Add(newCardSetMetadata);

        await JsonFileWriter.WriteToJsonAsync(newCardSetMetadata, destinationSetMetadataFilePath, cancellationToken);

        Log.Verbose($"New serialized Card Set metadata written to {destinationSetMetadataFilePath}");
        Log.Verbose(
            $"New Set: '{newCardSetMetadata.Id}' saved to path: '{newCardSetMetadata.DestinationAbsoluteFolderPath}'");

        await _handler.WriteMetadataToFileAsync(cancellationToken);
    }
}