using CardmastersOfTamriel.ImageProcessor.Configuration;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Noggog;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Setup;

public class DestinationFolderPreparer(PathSettings settings)
{
    public HashSet<string> GatherAllSourceSetFolders()
    {
        var folders = new HashSet<string>();
        foreach (var tierSourceFolderPath in Directory.EnumerateDirectories(settings.SourceImagesFolderPath, "*",
                     SearchOption.TopDirectoryOnly))
        {
            Enum.Parse<CardTier>(Path.GetFileName(tierSourceFolderPath));
            var seriesSourceFolders = Directory.EnumerateDirectories(tierSourceFolderPath).Order();
            folders.Add(seriesSourceFolders.SelectMany(Directory.EnumerateDirectories));
        }

        return folders;
    }

    public async Task<MasterMetadata> SetupDestinationFoldersAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var metadata = new MasterMetadata();
        foreach (var tier in Enum.GetValues<CardTier>())
        {
            metadata.Metadata[tier] = [];
        }

        FileOperations.EnsureDirectoryExists(settings.OutputFolderPath);

        foreach (var tierSourceFolderPath in Directory.EnumerateDirectories(settings.SourceImagesFolderPath, "*", SearchOption.TopDirectoryOnly))
        {
            var tierDestinationFolderPath = Path.Combine(settings.OutputFolderPath ?? string.Empty, Path.GetFileName(tierSourceFolderPath));
            FileOperations.EnsureDirectoryExists(tierDestinationFolderPath);

            var cardTier = Enum.Parse<CardTier>(Path.GetFileName(tierSourceFolderPath));
            var seriesSourceFolders = Directory.EnumerateDirectories(tierSourceFolderPath).Order();

            foreach (var seriesSourceFolderPath in seriesSourceFolders)
            {
                var seriesId = $"{Path.GetFileName(seriesSourceFolderPath)}-{Guid.NewGuid()}";
                var seriesFolder = Path.Combine(tierDestinationFolderPath, Path.GetFileName(seriesSourceFolderPath));
                FileOperations.EnsureDirectoryExists(seriesFolder);

                var seriesDestinationMetadataFilePath = Path.Combine(seriesFolder, PathSettings.DefaultFilenameForSeriesMetadataJson);
                var groupedFolders = DetermineFolderGrouping(seriesSourceFolderPath, cancellationToken);
                ProcessAndGetGroupedFolders(groupedFolders, seriesId, seriesSourceFolderPath);

                await CreateAndSaveSeriesMetadataAsync(seriesId, cardTier, seriesDestinationMetadataFilePath, seriesSourceFolderPath, seriesFolder,
                    groupedFolders, metadata, cancellationToken);
            }
        }

        var unprocessedFolders = GetUnprocessedFolders(metadata);
        if (unprocessedFolders.Count > 0) return metadata;

        Log.Warning("The following folders were not processed:");
        foreach (var folder in unprocessedFolders) Log.Warning(folder);

        return metadata;
    }

    private List<string> GetUnprocessedFolders(MasterMetadata metadata)
    {
        var allSourceSetFolders = GatherAllSourceSetFolders();

        var processedFolders = metadata.Metadata.Values
            .SelectMany(series => series.SelectMany(s =>
                s.Sets?.Where(set => !string.IsNullOrWhiteSpace(set.SourceAbsoluteFolderPath)).Select(set => set.SourceAbsoluteFolderPath) ??
                new List<string>()))
            .ToHashSet();

        return allSourceSetFolders.Except(processedFolders).ToList();
    }

    private static async Task CreateAndSaveSeriesMetadataAsync(string seriesId,
        CardTier cardTier,
        string seriesDestinationMetadataFilePath,
        string seriesSourceFolderPath,
        string seriesDestinationFolderPath,
        (Dictionary<string, List<string>>, HashSet<string>) groupedFolders,
        MasterMetadata metadata,
        CancellationToken cancellationToken)
    {
        try
        {
            var seriesMetadata = await CreateCardSeriesAndSaveToDisk(seriesId, cardTier,
                seriesDestinationMetadataFilePath, seriesSourceFolderPath, seriesDestinationFolderPath,
                cancellationToken);

            // With the series created, let's create the card sets
            var replicator = new CardSetReplicator(seriesMetadata);
            await replicator.HandleDestinationSetCreationAsync(groupedFolders.Item1, cancellationToken);

            if (seriesMetadata.Sets is null || seriesMetadata.Sets.Count == 0) 
                Log.Error($"{seriesId}:\tNo CardSets found for Series in Metadata");

            if (!metadata.Metadata.TryGetValue(cardTier, out var seriesMetadataList))
                metadata.Metadata[cardTier] = [];

            metadata.Metadata[cardTier].Add(seriesMetadata);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{seriesId}:\tFailed to create Series Metadata file at Destination Path: {seriesDestinationMetadataFilePath}");
        }
    }

    private static void ProcessAndGetGroupedFolders(
        (Dictionary<string, List<string>>, HashSet<string>) groupedFolders, string seriesId, string seriesSourceFolderPath)
    {
        if (groupedFolders.Item1.Count == 0) Log.Error($"{seriesId}:\tNo folder groups found in '{seriesSourceFolderPath}'");
        Log.Error($"{seriesId}:\tFound {groupedFolders.Item2.Count} folder name anomalies in '{seriesSourceFolderPath}':");
        foreach (var anomaly in groupedFolders.Item2) Log.Error($"{seriesId}:\t{anomaly}");
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
            seriesMetadata = await JsonFileReader.ReadFromJsonAsync<CardSeries?>(seriesDestinationMetadataFilePath, cancellationToken);
        else
            Log.Debug($"{seriesId}:\tDid not find an existing Series Metadata file at Destination Path: '{seriesDestinationMetadataFilePath}'");

        seriesMetadata ??= new CardSeries(seriesId)
        {
            DisplayName = NamingHelper.FormatDisplayNameFromFolderName(seriesDestinationFolderPath),
            Tier = tier,
            Description = string.Empty,
            Sets = [],
            SourceFolderPath = seriesSourceFolderPath,
            DestinationFolderPath = seriesDestinationFolderPath
        };

        // Refresh the folder paths in case they were changed 
        seriesMetadata.SourceFolderPath = seriesSourceFolderPath;
        seriesMetadata.DestinationFolderPath = seriesDestinationFolderPath;

        await JsonFileWriter.WriteToJsonAsync(seriesMetadata, seriesDestinationMetadataFilePath, cancellationToken);
        Log.Debug($"{seriesId}:\tSerialized Card Series metadata written to {seriesDestinationMetadataFilePath}");

        return seriesMetadata;
    }
}