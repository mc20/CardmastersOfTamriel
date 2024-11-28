using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Noggog;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Setup;

public class DestinationFolderPreparer
{
    private readonly Config _config;

    public DestinationFolderPreparer(Config config)
    {
        _config = config;
    }

    public HashSet<string> GatherAllSourceSetFolders()
    {
        var folders = new HashSet<string>();
        foreach (var tierSourceFolderPath in Directory.EnumerateDirectories(_config.Paths.SourceImagesFolderPath, "*",
                     SearchOption.TopDirectoryOnly))
        {
            Enum.Parse<CardTier>(Path.GetFileName(tierSourceFolderPath));
            var seriesSourceFolders = Directory.EnumerateDirectories(tierSourceFolderPath).Order();
            folders.Add(seriesSourceFolders.SelectMany(Directory.EnumerateDirectories));
        }

        return folders;
    }

    public async Task<HashSet<CardSeries>> SetupDestinationFoldersAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var allSeriesMetadata = new HashSet<CardSeries>();

        FileOperations.EnsureDirectoryExists(_config.Paths.OutputFolderPath);

        foreach (var tierSourceFolderPath in Directory.EnumerateDirectories(_config.Paths.SourceImagesFolderPath, "*",
                     SearchOption.TopDirectoryOnly))
        {
            var tierDestinationFolderPath = Path.Combine(_config.Paths.OutputFolderPath ?? string.Empty,
                Path.GetFileName(tierSourceFolderPath));
            FileOperations.EnsureDirectoryExists(tierDestinationFolderPath);

            var cardTier = Enum.Parse<CardTier>(Path.GetFileName(tierSourceFolderPath));
            var seriesSourceFolders = Directory.EnumerateDirectories(tierSourceFolderPath).Order();

            foreach (var seriesSourceFolderPath in seriesSourceFolders)
            {
                var seriesId = $"{Path.GetFileName(seriesSourceFolderPath)}-{Guid.NewGuid()}";
                var seriesDestinationFolderPath =
                    Path.Combine(tierDestinationFolderPath, Path.GetFileName(seriesSourceFolderPath));
                FileOperations.EnsureDirectoryExists(seriesDestinationFolderPath);

                var groupedFolders = DetermineFolderGrouping(seriesSourceFolderPath, cancellationToken);
                if (groupedFolders.Item1.Count == 0)
                {
                    Log.Error($"{seriesId}:\tNo folder groups found in '{seriesSourceFolderPath}'");
                }

                if (groupedFolders.Item2.Count > 0)
                {
                    Log.Error(
                        $"{seriesId}:\tFound {groupedFolders.Item2.Count} folder name anomalies in '{seriesSourceFolderPath}':");
                    foreach (var anomaly in groupedFolders.Item2)
                    {
                        Log.Error($"{seriesId}:\t{anomaly}");
                    }
                }

                var seriesDestinationMetadataFilePath = Path.Combine(seriesDestinationFolderPath,
                    PathSettings.DefaultFilenameForSeriesMetadataJson);

                try
                {
                    var seriesMetadata = await CreateCardSeriesAndSaveToDisk(seriesId, cardTier,
                        seriesDestinationMetadataFilePath, seriesSourceFolderPath, seriesDestinationFolderPath,
                        cancellationToken);

                    // With the series created, let's create the card sets
                    var replicator = new CardSetReplicator(seriesMetadata, _config);
                    await replicator.HandleDestinationSetCreationAsync(groupedFolders.Item1, cancellationToken);

                    if (seriesMetadata.Sets is null || seriesMetadata.Sets.Count == 0)
                    {
                        Log.Error($"{seriesId}:\tNo CardSets found for Series in Metadata");
                    }

                    allSeriesMetadata.Add(seriesMetadata);
                }
                catch (Exception ex)
                {
                    Log.Error(ex,
                        $"{seriesId}:\tFailed to create Series Metadata file at Destination Path: {seriesDestinationMetadataFilePath}");
                }
            }
        }

        var allSourceSetFolders = GatherAllSourceSetFolders();
        var processedFolders = allSeriesMetadata
            .SelectMany(series => series.Sets?.Select(set => set.SourceAbsoluteFolderPath) ?? []).ToHashSet();

        var unprocessedFolders = allSourceSetFolders.Except(processedFolders).ToList();
        if (unprocessedFolders.Count <= 0) return allSeriesMetadata;

        Log.Warning("The following folders were not processed:");
        foreach (var folder in unprocessedFolders)
        {
            Log.Warning(folder);
        }

        return allSeriesMetadata;
    }

    private static (Dictionary<string, List<string>>, HashSet<string>) DetermineFolderGrouping(
        string seriesSourceFolderPath, CancellationToken cancellationToken = default)
    {
        var groupedFolders = new Dictionary<string, List<string>>();
        var folderNameAnomalies = new HashSet<string>();

        foreach (var setSourceFolderPath in Directory.EnumerateDirectories(seriesSourceFolderPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            folderNameAnomalies = CardSetHelper.GroupAndNormalizeFolderNames(setSourceFolderPath, groupedFolders);
        }

        return (groupedFolders, folderNameAnomalies);
    }

    private static async Task<CardSeries> CreateCardSeriesAndSaveToDisk(string seriesId, CardTier tier,
        string seriesDestinationMetadataFilePath, string seriesSourceFolderPath, string seriesDestinationFolderPath,
        CancellationToken cancellationToken)
    {
        CardSeries? seriesMetadata = null;
        if (File.Exists(seriesDestinationMetadataFilePath))
        {
            seriesMetadata =
                await JsonFileReader.ReadFromJsonAsync<CardSeries?>(seriesDestinationMetadataFilePath,
                    cancellationToken);
        }
        else
        {
            Log.Verbose(
                $"{seriesId}:\tDid not find an existing Series Metadata file at Destination Path: '{seriesDestinationMetadataFilePath}'");
        }

        seriesMetadata ??= new CardSeries(seriesId)
        {
            DisplayName = NamingHelper.FormatDisplayNameFromFolderName(seriesDestinationFolderPath),
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
        Log.Verbose($"{seriesId}:\tSerialized Card Series metadata written to {seriesDestinationMetadataFilePath}");

        return seriesMetadata;
    }
}