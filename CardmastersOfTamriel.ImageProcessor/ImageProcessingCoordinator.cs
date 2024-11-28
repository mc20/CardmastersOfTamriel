using System.Collections.Concurrent;
using System.Diagnostics;
using CardmastersOfTamriel.ImageProcessor.CardSets;
using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.ImageProcessor.Setup;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;
using ShellProgressBar;

namespace CardmastersOfTamriel.ImageProcessor;

public class ImageProcessingCoordinator
{
    private readonly Config _config;
    private readonly CancellationToken _cancellationToken;
    private readonly ConcurrentDictionary<string, CardOverrideData>? _overrideData;

    private ProgressBar? _progressBarForCardSetHandlers;
    private ProgressBar? _progressBarForFolderPreparer;

    private readonly ProgressTracker _progressTrackerForOverallCardSetHandlers;
    private readonly ProgressTracker _progressTrackerForFolderPreparer;

    public ImageProcessingCoordinator(Config config, CancellationToken cancellationToken,
        ConcurrentDictionary<string, CardOverrideData>? overrideData = null)
    {
        _cancellationToken = cancellationToken;
        _progressTrackerForOverallCardSetHandlers = new ProgressTracker();
        _progressTrackerForFolderPreparer = new ProgressTracker();
        _config = config;
        _overrideData = overrideData;
    }

    public async Task PerformProcessingUsingHandlerAsync(ICardSetHandler handler)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var preparer = new DestinationFolderPreparer(_config);

            _progressTrackerForFolderPreparer.Total = preparer.GatherAllSourceSetFolders().Count;

            HashSet<CardSet> allCardSets;

            using (_progressBarForFolderPreparer = new ProgressBar(_progressTrackerForFolderPreparer.Total, "Folder Preparation (setCount)"))
            {
                EventBroker.FolderPreparationProgressUpdated += OnFolderPreparerProgressUpdated;
                var allCardSeries = await preparer.SetupDestinationFoldersAsync(_cancellationToken);
                allCardSets = allCardSeries.SelectMany(series => series.Sets ?? []).ToHashSet();
                _progressTrackerForOverallCardSetHandlers.Total = GetAbsoluteTotalNumberOfImagesOnDiskAtSource(allCardSets);
            }

            using (_progressBarForCardSetHandlers = new ProgressBar(_progressTrackerForOverallCardSetHandlers.Total, "Overall Progress"))
            {
                EventBroker.SetHandlingProgressUpdated += OnCardSetHandlerProgressUpdated;

                await Parallel.ForEachAsync(allCardSets, new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = _cancellationToken
                }, async (set, token) =>
                {
                    try
                    {
                        var dataOverride = _overrideData?.GetValueOrDefault(set.Id);
                        await handler.ProcessCardSetAsync(set, token, dataOverride);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "An error occurred while processing series");
                        throw;
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while processing series");
            throw;
        }
        finally
        {
            EventBroker.SetHandlingProgressUpdated -= OnCardSetHandlerProgressUpdated;
            EventBroker.FolderPreparationProgressUpdated -= OnFolderPreparerProgressUpdated;
        }
    }

    public async Task CompileSeriesMetadataAsync()
    {
        _cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Log.Information("Compiling series metadata");

            var completeMetadata = new Dictionary<CardTier, HashSet<CardSeries>>();

            // Sets
            var allLeafFolders = Directory
                .EnumerateDirectories(_config.Paths.OutputFolderPath, "*", SearchOption.AllDirectories)
                .Where(dir => !Directory.EnumerateDirectories(dir).Any())
                .ToList();

            var compilationProgressTracker = new ProgressTracker
            {
                Total = allLeafFolders.Count
            };

            using var compileProgressBar = new ProgressBar(compilationProgressTracker.Total, "Compiling Master Metadata (setCount)");

            var tierDirectories = Directory.EnumerateDirectories(_config.Paths.OutputFolderPath, "*", SearchOption.TopDirectoryOnly).ToList();
            foreach (var tierFolder in tierDirectories)
            {
                foreach (var seriesFolder in Directory.EnumerateDirectories(tierFolder, "*", SearchOption.TopDirectoryOnly))
                {
                    Log.Verbose("Processing series folder: {SeriesFolder}", seriesFolder);

                    var seriesMetadataFile = Path.Combine(seriesFolder, PathSettings.DefaultFilenameForSeriesMetadataJson);
                    if (!File.Exists(seriesMetadataFile))
                    {
                        Log.Warning("Series metadata file not found: {SeriesMetadataFile}", seriesMetadataFile);
                        continue;
                    }

                    var series = await JsonFileReader.ReadFromJsonAsync<CardSeries>(seriesMetadataFile, _cancellationToken);

                    if (!completeMetadata.TryGetValue(series.Tier, out var cardSeries))
                    {
                        completeMetadata[series.Tier] = [];
                    }

                    completeMetadata[series.Tier].Add(series);

                    foreach (var setFolder in Directory.EnumerateDirectories(seriesFolder, "*",
                                 SearchOption.TopDirectoryOnly))
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        var setMetadataFile = Path.Combine(setFolder, PathSettings.DefaultFilenameForSetMetadataJson);
                        if (!File.Exists(setMetadataFile))
                        {
                            Log.Warning("Set metadata file not found: {SetMetadataFile}", setMetadataFile);
                            continue;
                        }

                        var set = await JsonFileReader.ReadFromJsonAsync<CardSet>(setMetadataFile, _cancellationToken);
                        series.Sets ??= [];
                        series.Sets.Add(set);

                        if (compileProgressBar is not null)
                            UpdateProgressTracker(compilationProgressTracker, compileProgressBar, "Processing all CardSet metadata files");
                    }
                }
            }

            await JsonFileWriter.WriteToJsonAsync(completeMetadata, _config.Paths.MasterMetadataFilePath, _cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while compiling series metadata");
            throw;
        }
    }

    public async Task CleanupNonTrackedFilesAtDestination()
    {
        _cancellationToken.ThrowIfCancellationRequested();

        try
        {
            foreach (var seriesFolder in Directory.EnumerateDirectories(_config.Paths.OutputFolderPath, "*", SearchOption.AllDirectories))
            {
                foreach (var setFolder in Directory.EnumerateDirectories(seriesFolder, "*", SearchOption.TopDirectoryOnly))
                {
                    var setMetadataFile = Path.Combine(setFolder, PathSettings.DefaultFilenameForCardsJsonl);
                    if (!File.Exists(setMetadataFile)) continue;

                    var cards = await JsonFileReader.LoadAllFromJsonLineFileAsync<Card>(setMetadataFile, _cancellationToken);

                    var destinations = cards.Select(card => card.DestinationAbsoluteFilePath).ToHashSet();
                    var imagePathsInFolder = ImageFilePathUtility.GetImageFilePathsFromFolder(setFolder, ["*.png", "*.jpg", "*.jpeg", "*.dds"]);
                    var nonTrackedFiles = imagePathsInFolder.Except(destinations).ToHashSet();

                    foreach (var nonTrackedFile in nonTrackedFiles)
                    {
                        if (string.IsNullOrWhiteSpace(nonTrackedFile)) continue;

                        Log.Information("Deleting non-tracked file: {nonTrackedFile}", nonTrackedFile);
                        File.Delete(nonTrackedFile);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "An error occurred while cleaning up non-tracked files at destination");
        }
    }

    private void OnCardSetHandlerProgressUpdated(object? sender, ProgressTrackingEventArgs? e)
    {
        if (e == null) return;

        if (_progressBarForCardSetHandlers is not null)
            UpdateProgressTracker(_progressTrackerForOverallCardSetHandlers, _progressBarForCardSetHandlers, "Processing all Cards");
    }

    private static void UpdateProgressTracker(ProgressTracker tracker, ProgressBar progressBar, string message = "")
    {
        // Start the stopwatch on the first update
        if (tracker.Current == 0)
            tracker.ProgressStopwatch.Start();

        tracker.Current++;

        var estimatedTimeLeft = tracker.CalculateEstimatedTimeLeft();

        // Update the progress bar with the corrected ETA
        progressBar?.Tick(
            $"{message}: {tracker.Current}/{tracker.Total} [ETA: {TextFormattingHelper.FormatDuration((long)estimatedTimeLeft)}]"
        );

        // Stop the stopwatch if processing is complete
        if (tracker.IsComplete)
            tracker.ProgressStopwatch.Stop();
    }

    private void OnFolderPreparerProgressUpdated(object? sender, ProgressTrackingEventArgs? e)
    {
        if (e == null) return;

        // Start the stopwatch on the first update
        if (_progressTrackerForFolderPreparer.Current == 0)
            _progressTrackerForFolderPreparer.ProgressStopwatch.Start();

        _progressTrackerForFolderPreparer.Current++;

        var estimatedTimeLeft = _progressTrackerForFolderPreparer.CalculateEstimatedTimeLeft();

        // Update the progress bar with the corrected ETA
        _progressBarForFolderPreparer?.Tick(
            $"Processing all set folders: {_progressTrackerForFolderPreparer.Current}/{_progressTrackerForFolderPreparer.Total} [ETA: {TextFormattingHelper.FormatDuration((long)estimatedTimeLeft)}]"
        );

        // Stop the stopwatch if processing is complete
        if (_progressTrackerForFolderPreparer.IsComplete)
            _progressTrackerForFolderPreparer.ProgressStopwatch.Stop();
    }

    private static int GetAbsoluteTotalNumberOfImagesOnDiskAtSource(HashSet<CardSet> allCardSets)
    {
        return allCardSets.Sum(set =>
            ImageFilePathUtility.GetImageFilePathsFromFolder(set.SourceAbsoluteFolderPath).Count);
    }

    public static int GetMaximumNumberOfCardsToInclude(CardTier tier, int totalNumberOfCards, Config config)
    {
        if (totalNumberOfCards < 0) return 0;

        if (tier == CardTier.Tier4) return totalNumberOfCards;

        var calculatedMaximumNumberOfCards = (int)Math.Ceiling(totalNumberOfCards * config.General.MaximumImageSelectionPercentageForSet);
        return calculatedMaximumNumberOfCards <= config.General.MinimumImageSelectionCountForSet ? config.General.MinimumImageSelectionCountForSet : calculatedMaximumNumberOfCards;
    }
}