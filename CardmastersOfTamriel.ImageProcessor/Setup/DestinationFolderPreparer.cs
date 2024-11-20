using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Setup;

public static class DestinationFolderPreparer
{
    public static int GetTotalFolderCountAtSource() => Directory
        .EnumerateDirectories(ConfigurationProvider.Instance.Config.Paths.SourceImagesFolderPath, "*",
            SearchOption.AllDirectories).Count();
    
    public static async Task<HashSet<CardSeries>> SetupDestinationFoldersAsync(CancellationToken cancellationToken)
    {
        var config = ConfigurationProvider.Instance.Config;

        var allSeriesMetadata = new HashSet<CardSeries>();

        FileOperations.EnsureDirectoryExists(config.Paths.OutputFolderPath);

        foreach (var tierSourceFolderPath in Directory.EnumerateDirectories(config.Paths.SourceImagesFolderPath))
        {
            var tierDestinationFolderPath = Path.Combine(config.Paths.OutputFolderPath ?? string.Empty, Path.GetFileName(tierSourceFolderPath));
            FileOperations.EnsureDirectoryExists(tierDestinationFolderPath);

            var cardTier = Enum.Parse<CardTier>(Path.GetFileName(tierSourceFolderPath));
            var seriesSourceFolders = Directory.EnumerateDirectories(tierSourceFolderPath).Order();

            foreach (var seriesSourceFolderPath in seriesSourceFolders)
            {
                var seriesId = Path.GetFileName(seriesSourceFolderPath);
                var seriesDestinationFolderPath = Path.Combine(tierDestinationFolderPath, seriesId);
                FileOperations.EnsureDirectoryExists(seriesDestinationFolderPath);

                var groupedFolders = DetermineFolderGrouping(seriesSourceFolderPath, cancellationToken);
                if (groupedFolders.Count == 0)
                {
                    Log.Warning($"{seriesId}\tNo folder groups found in '{seriesSourceFolderPath}'");
                    continue;
                }

                const string seriesMetadataFileName = "series_metadata.json";
                var seriesDestinationMetadataFilePath = Path.Combine(seriesDestinationFolderPath, seriesMetadataFileName);

                var seriesMetadata = await CreateCardSeriesAndSaveToDisk(seriesId,
                    cardTier,
                    seriesDestinationMetadataFilePath,
                    seriesSourceFolderPath,
                    seriesDestinationFolderPath,
                    cancellationToken);

                var replicator = new CardSetReplicator(seriesMetadata);
                
                await replicator.HandleDestinationSetCreationAsync(groupedFolders, cancellationToken);

                if (seriesMetadata.Sets is null || seriesMetadata.Sets.Count == 0)
                {
                    Log.Warning($"{seriesId}\tNo CardSets found for Series in Metadata");
                }

                allSeriesMetadata.Add(seriesMetadata);
            }
        }

        return allSeriesMetadata;
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

    private static async Task<CardSeries> CreateCardSeriesAndSaveToDisk(string seriesId, CardTier tier, string seriesDestinationMetadataFilePath, string seriesSourceFolderPath, string seriesDestinationFolderPath, CancellationToken cancellationToken)
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