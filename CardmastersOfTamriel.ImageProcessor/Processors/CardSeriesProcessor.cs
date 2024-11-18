using System.Text.Json;
using CardmastersOfTamriel.ImageProcessor.CardSets;
using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Processors;

[Obsolete("Use CardSeriesProcessorAsync instead", false)]
public class CardSeriesProcessor
{
    private readonly MasterMetadataHandler _handler = MasterMetadataProvider.Instance.MetadataHandler;

    public CardSeriesProcessor()
    {
    }

    [Obsolete("Use ProcessSeriesFolderAsync instead", false)]
    public void ProcessSeriesFolder(CardTier tier, string seriesSourceFolderPath,
        string tierDestinationFolderPath, ICardSetHandler cardSetHandler)
    {
        Log.Information($"Examining Source Series folder: '{seriesSourceFolderPath}'");

        var seriesId = Path.GetFileName(seriesSourceFolderPath);

        var seriesDestinationFolderPath = Path.Combine(tierDestinationFolderPath, seriesId);

        FileOperations.EnsureDirectoryExists(seriesDestinationFolderPath);

        _handler.WriteMetadataToFile(); // Save progress

        Log.Verbose($"{seriesId}\tDetermining folder grouping at path: '{seriesDestinationFolderPath}'");

        var groupedFolders = DetermineFolderGrouping(seriesSourceFolderPath);

        if (groupedFolders.Count == 0)
        {
            Log.Warning($"{seriesId}\tNo folder groups found in '{seriesSourceFolderPath}'");
            _handler.WriteMetadataToFile();
            return;
        }

        const string seriesMetadataFileName = "series_metadata.json";
        var seriesDestinationMetadataFilePath = Path.Combine(seriesDestinationFolderPath, seriesMetadataFileName);

        CardSeries? seriesMetadata = null;
        if (File.Exists(seriesDestinationMetadataFilePath))
        {
            seriesMetadata = JsonFileReader.ReadFromJson<CardSeries?>(seriesDestinationMetadataFilePath);
        }
        else
        {
            Log.Verbose(
                $"{seriesId}\tDid not find an existing Series Metadata file at Destination Path: '{seriesDestinationMetadataFilePath}'");
        }

        seriesMetadata ??= new CardSeries(seriesId)
        {
            DisplayName = NameHelper.FormatDisplayNameFromId(seriesId),
            Tier = tier,
            Description = string.Empty,
            Sets = [],
            SourceFolderPath = seriesSourceFolderPath,
            DestinationFolderPath = seriesDestinationFolderPath,
        };

        // Refresh the folder paths in case they were changed 
        seriesMetadata.SourceFolderPath = seriesSourceFolderPath;
        seriesMetadata.DestinationFolderPath = seriesDestinationFolderPath;

        _handler.Metadata.Series ??= [];
        _handler.Metadata.Series.Add(seriesMetadata);

        var serializedJson = JsonSerializer.Serialize(seriesMetadata, JsonSettings.Options);
        File.WriteAllText(seriesDestinationMetadataFilePath, serializedJson);
        Log.Debug($"{seriesId}\tSerialized Card Series metadata written to {seriesDestinationMetadataFilePath}");

        var replicator = new CardSetReplicator(seriesId);
        replicator.HandleDestinationSetCreation(groupedFolders);

        if (seriesMetadata?.Sets is null || seriesMetadata.Sets.Count == 0)
        {
            Log.Warning($"{seriesId}\tNo CardSets found for Series in Metadata");
            return;
        }

        if (!File.Exists(ConfigurationProvider.Instance.Config.Paths.RebuildListFilePath))
        {
            Log.Warning("Rebuild list file does not exist at the specified path. Creating a empty placeholder..");

            var newRebuildList = new Dictionary<string, string>();
            var rebuildListFilePath = ConfigurationProvider.Instance.Config.Paths.RebuildListFilePath;
            var emptyJson = JsonSerializer.Serialize(newRebuildList, JsonSettings.Options);
            File.WriteAllText(rebuildListFilePath, emptyJson);
            Log.Information($"Created empty rebuild list file at {rebuildListFilePath}");
        }

        var rebuildlist =
            JsonFileReader.ReadFromJson<Dictionary<string, string>>(ConfigurationProvider.Instance.Config.Paths
                .RebuildListFilePath);
        foreach (var cardSet in seriesMetadata.Sets)
        {
            if (rebuildlist.Count > 0)
            {
                if (!rebuildlist.TryGetValue(cardSet.Id, out var rebuildSeriesId) ||
                    rebuildSeriesId != cardSet.SeriesId)
                {
                    Log.Information(
                        $"{cardSet.Id}\tSkipping rebuild as set is not in rebuild list or series ID does not match");
                    continue;
                }
            }

            cardSetHandler.ProcessCardSet(cardSet);
        }

        _handler.WriteMetadataToFile();
    }

    private static Dictionary<string, List<string>> DetermineFolderGrouping(string seriesSourceFolderPath, CancellationToken cancellationToken = default)
    {
        var groupedFolders = new Dictionary<string, List<string>>();

        foreach (var setSourceFolderPath in Directory.EnumerateDirectories(seriesSourceFolderPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            CardSetHelper.GroupAndNormalizeFolderNames(setSourceFolderPath, groupedFolders);
        }

        return groupedFolders;
    }
}