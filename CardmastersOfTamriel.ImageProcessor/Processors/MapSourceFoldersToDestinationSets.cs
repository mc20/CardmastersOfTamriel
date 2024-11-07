using System.Text.Json;
using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Processors;

public static class MapSourceFoldersToDestinationSets
{
    public static void BeginProcessing()
    {
        var config = ConfigurationProvider.Instance.Config;
        var handler = MasterMetadataProvider.Instance.MetadataHandler;
        
        FileOperations.EnsureDirectoryExists(config.Paths.OutputFolderPath);

        foreach (var tierSourceFolderPath in
                 Directory.EnumerateDirectories(config.Paths.SourceImagesFolderPath))
        {
            Log.Information($"Examining Tier Source Folder Path: '{tierSourceFolderPath}'");

            var tierDestinationFolderPath = Path.Combine(config.Paths.OutputFolderPath ?? string.Empty,
                Path.GetFileName(tierSourceFolderPath));
            FileOperations.EnsureDirectoryExists(tierDestinationFolderPath);

            var cardTier = Enum.Parse<CardTier>(Path.GetFileName(tierSourceFolderPath));
                
            foreach(var seriesSourceFolderPath in Directory.EnumerateDirectories(tierSourceFolderPath))
            {
                Log.Information($"Examining Source Series folder: '{seriesSourceFolderPath}'");
                
                var seriesId = Path.GetFileName(seriesSourceFolderPath);
                
                var seriesDestinationFolderPath = Path.Combine(tierDestinationFolderPath, seriesId);
             
                FileOperations.EnsureDirectoryExists(seriesDestinationFolderPath);
                
                Log.Verbose($"Determining folder grouping at path: '{seriesDestinationFolderPath}'");
                
                var groupedFolders = DetermineFolderGrouping(seriesSourceFolderPath);
                
                if (groupedFolders.Count == 0)
                {
                    Log.Warning($"No folder groups found in '{seriesSourceFolderPath}'");
                    return;
                }

                const string seriesMetadataFileName = "series_metadata.json";
                var seriesDestinationMetadataFilePath = Path.Combine(seriesDestinationFolderPath, seriesMetadataFileName);
                
                var seriesMetadata = JsonFileReader.ReadFromJson<CardSeries?>(seriesDestinationMetadataFilePath);
                if (seriesMetadata == null)
                {
                    seriesMetadata = new CardSeries(seriesId)
                    {
                        DisplayName = NameHelper.FormatDisplayNameFromId(seriesId),
                        Tier = cardTier,
                        Description = string.Empty,
                        Sets = [],
                        SourceFolderPath = seriesSourceFolderPath,
                        DestinationFolderPath = seriesDestinationFolderPath,
                    };
                }

                // Refresh the folder paths in case they were changed 
                seriesMetadata.SourceFolderPath = seriesSourceFolderPath;
                seriesMetadata.DestinationFolderPath = seriesDestinationFolderPath;

                handler.Metadata.Series ??= [];
                handler.Metadata.Series.Add(seriesMetadata);
                
                var serializedJson = JsonSerializer.Serialize(seriesMetadata, JsonSettings.Options);
                File.WriteAllText(seriesDestinationMetadataFilePath, serializedJson);
                Log.Information($"Serialized Card Series metadata written to {seriesDestinationMetadataFilePath}");
                
                var replicator = new CardSetReplicator(seriesId);
                replicator.HandleDestinationSetCreation(groupedFolders);
                
                
              
                handler.WriteMetadataToFile();
            }
        }
    }

    private static Dictionary<string, List<string>> DetermineFolderGrouping(string seriesSourceFolderPath)
    {
        var groupedFolders = new Dictionary<string, List<string>>();

        foreach (var setSourceFolderPath in Directory.EnumerateDirectories(seriesSourceFolderPath))
        {
            CardSetHelper.GroupAndNormalizeFolderNames(setSourceFolderPath, groupedFolders);
        }

        return groupedFolders;
    }
}