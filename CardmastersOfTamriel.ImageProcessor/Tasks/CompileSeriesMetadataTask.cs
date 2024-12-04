using CardmastersOfTamriel.ImageProcessor.Configuration;
using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;
using ShellProgressBar;

namespace CardmastersOfTamriel.ImageProcessor.Tasks;

public class CompileSeriesMetadataTask
{
    private ProgressBar? _compileProgressBar;
    private ProgressTracker _compilationProgressTracker = new();

    public async Task WriteToDiskAsync(PathSettings pathSettings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Log.Information("Compiling series metadata");

        var masterMetadata = new MasterMetadata();

        try
        {
            // Sets
            var allLeafFolders = Directory
                .EnumerateDirectories(pathSettings.OutputFolderPath, "*", SearchOption.AllDirectories)
                .Where(dir => !Directory.EnumerateDirectories(dir).Any() && !dir.EndsWith("Logs"))
                .ToList();

            _compilationProgressTracker = new ProgressTracker
            {
                Current = 0,
                Total = allLeafFolders.Count
            };

            using (_compileProgressBar =
                       new ProgressBar(_compilationProgressTracker.Total, $"Compiling Master Metadata ({_compilationProgressTracker.Total})"))
            {
                var tierDirectories = Directory.EnumerateDirectories(pathSettings.OutputFolderPath, "*", SearchOption.TopDirectoryOnly).ToList();
                foreach (var tierFolder in tierDirectories)
                foreach (var seriesFolder in Directory.EnumerateDirectories(tierFolder, "*", SearchOption.TopDirectoryOnly))
                {
                    Log.Debug("Processing series folder: {SeriesFolder}", seriesFolder);

                    var seriesMetadataFile = Path.Combine(seriesFolder, PathSettings.DefaultFilenameForSeriesMetadataJson);
                    if (!File.Exists(seriesMetadataFile))
                    {
                        Log.Warning("Series metadata file not found: {SeriesMetadataFile}", seriesMetadataFile);
                        continue;
                    }

                    var series = await JsonFileReader.ReadFromJsonAsync<CardSeries>(seriesMetadataFile, cancellationToken);

                    if (!masterMetadata.Metadata.TryGetValue(series.Tier, out var cardSeries)) masterMetadata.Metadata[series.Tier] = [];

                    masterMetadata.Metadata[series.Tier].Add(series);

                    foreach (var setFolder in Directory.EnumerateDirectories(seriesFolder, "*", SearchOption.TopDirectoryOnly))
                    {
                        Log.Debug("Processing set folder: {SetFolder}", setFolder);

                        var set = await ProcessSetFolder(setFolder, cancellationToken);

                        Log.Debug("Adding set to series: {SetName}", set?.DisplayName);

                        series.Sets ??= [];
                        if (set is not null) series.Sets.Add(set);

                        Log.Debug("Set contains {CardCount} cards", set?.Cards?.Count ?? 0);

                        ProgressTrackerUpdater.UpdateProgressTracker(_compilationProgressTracker, _compileProgressBar, "Processing all CardSet metadata files");
                    }
                }
            }

            await JsonFileWriter.WriteToJsonAsync(masterMetadata, pathSettings.MasterMetadataFilePath, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while compiling series metadata");
            throw;
        }
    }

    private static async Task<CardSet?> ProcessSetFolder(string setFolder, CancellationToken cancellationToken)
    {
        var setMetadataFile = Path.Combine(setFolder, PathSettings.DefaultFilenameForSetMetadataJson);
        if (File.Exists(setMetadataFile)) return await JsonFileReader.ReadFromJsonAsync<CardSet>(setMetadataFile, cancellationToken);
        Log.Warning("Set metadata file not found: {SetMetadataFile}", setMetadataFile);
        return null;
    }
}