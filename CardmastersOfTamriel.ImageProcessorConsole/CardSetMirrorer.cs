using System.Text.Json;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public class CardSetMirrorer
{
    private readonly MasterMetadataHandler _handler;
    private readonly CardSeries _series;

    public CardSetMirrorer(MasterMetadataHandler handler, string seriesId)
    {
        // TODO: weird
        _handler = handler;
        _series = handler.Metadata.Series?.FirstOrDefault(series => series.Id == seriesId) ??
                  throw new KeyNotFoundException($"No series found with id: {seriesId}");
    }

    public void CreateSetsAtDestination(Dictionary<string, List<string>> groupedFolders)
    {
        foreach (var (setFolderName, sourceSetPaths) in groupedFolders)
        {
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

    private void SaveNewSetAndCreateAtDestination(string setFolderName, string destinationSetFolderPath,
        string sourceSetPath)
    {
        Directory.CreateDirectory(destinationSetFolderPath);

        _series.Sets?.RemoveAll(set => set.Id == _series.Id);

        CardSet? newCardSetMetadata = null;
            
        var destinationSetMetadataFilePath = Path.Combine(destinationSetFolderPath, "set_metadata.json");
        if (File.Exists(destinationSetMetadataFilePath))
        {
            try
            {
                 newCardSetMetadata = JsonFileReader.ReadFromJson<CardSet>(destinationSetMetadataFilePath);
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
            newCardSetMetadata.SeriesId = _series.Id;
            newCardSetMetadata.Tier = _series.Tier;
        }

        newCardSetMetadata.SourceFolderPath = sourceSetPath;
        newCardSetMetadata.DestinationFolderPath = destinationSetFolderPath;

        _series.Sets ??= [];
        _series.Sets.Add(newCardSetMetadata);

        var serializedJson = JsonSerializer.Serialize(newCardSetMetadata, JsonSettings.Options);
        File.WriteAllText(destinationSetMetadataFilePath, serializedJson);
        Log.Information($"New serialized Card Set metadata written to {destinationSetMetadataFilePath}");
        
        Log.Information($"New Set: '{newCardSetMetadata.Id}' saved to path: '{newCardSetMetadata.DestinationFolderPath}'");
        
        _handler.WriteMetadataToFile();
    }
}