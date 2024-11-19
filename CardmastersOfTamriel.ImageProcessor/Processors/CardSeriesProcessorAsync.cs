using CardmastersOfTamriel.ImageProcessor.CardSets;
using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Processors;

public static class CardSeriesProcessorAsync
{

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

    public static async Task ProcessSeriesFolderAsync(CardTier tier, string seriesSourceFolderPath,
        string tierDestinationFolderPath, ICardSetHandler asyncCardSetHandler,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log.Information($"Examining Source Series folder: '{seriesSourceFolderPath}'");

        var seriesId = Path.GetFileName(seriesSourceFolderPath);

        var seriesDestinationFolderPath = Path.Combine(tierDestinationFolderPath, seriesId);

        FileOperations.EnsureDirectoryExists(seriesDestinationFolderPath);

        Log.Verbose($"{seriesId}\tDetermining folder grouping at path: '{seriesDestinationFolderPath}'");
        var groupedFolders = DetermineFolderGrouping(seriesSourceFolderPath, cancellationToken);

        if (groupedFolders.Count == 0)
        {
            Log.Warning($"{seriesId}\tNo folder groups found in '{seriesSourceFolderPath}'");
            return;
        }

        const string seriesMetadataFileName = "series_metadata.json";
        var seriesDestinationMetadataFilePath = Path.Combine(seriesDestinationFolderPath, seriesMetadataFileName);

        var seriesMetadata = await AddNewSeriesToMetadataAsync(seriesId, tier, seriesDestinationMetadataFilePath, seriesSourceFolderPath, seriesDestinationFolderPath, cancellationToken);

        var replicator = new CardSetReplicatorAsync(seriesMetadata);
        await replicator.HandleDestinationSetCreationAsync(groupedFolders, cancellationToken);

        if (seriesMetadata.Sets is null || seriesMetadata.Sets.Count == 0)
        {
            Log.Warning($"{seriesId}\tNo CardSets found for Series in Metadata");
            return;
        }

        var rebuildlist = await GetRebuildListFileAsync(cancellationToken);

        await Parallel.ForEachAsync(seriesMetadata.Sets, new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        }, async (cardSet, token) =>
        {
            try
            {
                ReportRebuildListStatus(rebuildlist, cardSet);
                await asyncCardSetHandler.ProcessCardSetAsync(cardSet, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{cardSet.Id}\tFailed to process card set");
            }
        });
    }

    private static async Task<Dictionary<string, string>> GetRebuildListFileAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(ConfigurationProvider.Instance.Config.Paths.RebuildListFilePath))
        {
            Log.Warning("Rebuild list file does not exist at the specified path. Creating a empty placeholder..");

            var newRebuildList = new Dictionary<string, string>();
            var rebuildListFilePath = ConfigurationProvider.Instance.Config.Paths.RebuildListFilePath;
            await JsonFileWriter.WriteToJsonAsync(newRebuildList, rebuildListFilePath, cancellationToken);
            Log.Information($"Created empty rebuild list file at {rebuildListFilePath}");
        }

        return await JsonFileReader.ReadFromJsonAsync<Dictionary<string, string>>(
            ConfigurationProvider.Instance.Config.Paths.RebuildListFilePath, cancellationToken);
    }

    private static void ReportRebuildListStatus(Dictionary<string, string> rebuildlist, CardSet cardSet)
    {
        if (rebuildlist.Count > 0)
        {
            if (!rebuildlist.TryGetValue(cardSet.Id, out var rebuildSeriesId) ||
                rebuildSeriesId != cardSet.SeriesId)
            {
                Log.Information($"{cardSet.Id}\tSkipping rebuild as set is not in rebuild list or series ID does not match");
            }
        }
    }

    private static async Task<CardSeries> AddNewSeriesToMetadataAsync(string seriesId, CardTier tier, string seriesDestinationMetadataFilePath, string seriesSourceFolderPath, string seriesDestinationFolderPath, CancellationToken cancellationToken)
    {
        CardSeries? seriesMetadata = null;
        if (File.Exists(seriesDestinationMetadataFilePath))
        {
            seriesMetadata = await JsonFileReader.ReadFromJsonAsync<CardSeries?>(seriesDestinationMetadataFilePath, cancellationToken);
        }
        else
        {
            Log.Verbose($"{seriesId}\tDid not find an existing Series Metadata file at Destination Path: '{seriesDestinationMetadataFilePath}'");
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

        await JsonFileWriter.WriteToJsonAsync(seriesMetadata, seriesDestinationMetadataFilePath, cancellationToken);
        Log.Verbose($"{seriesId}\tSerialized Card Series metadata written to {seriesDestinationMetadataFilePath}");

        return seriesMetadata;
    }
}
