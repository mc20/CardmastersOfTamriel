using System.Collections.Concurrent;
using CardmastersOfTamriel.ImageProcessor.CardSets;
using CardmastersOfTamriel.ImageProcessor.Configuration;
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
    private readonly CancellationToken _cancellationToken;
    private readonly PathSettings _pathSettings;
    private readonly ConcurrentDictionary<CardTier, ConcurrentDictionary<string, ConcurrentDictionary<string, CardSetHandlerOverrideData>>>? _overrideData;
    private readonly ProgressTracker _progressTrackerForFolderPreparer;

    private readonly ProgressTracker _progressTrackerForOverallCardSetHandlers;

    private ProgressBar? _progressBarForCardSetHandlers;
    private ProgressBar? _progressBarForFolderPreparer;

    public ImageProcessingCoordinator(PathSettings pathSettings, CancellationToken cancellationToken,
        ConcurrentDictionary<CardTier, ConcurrentDictionary<string, ConcurrentDictionary<string, CardSetHandlerOverrideData>>>? overrideData = null)
    {
        _cancellationToken = cancellationToken;
        _progressTrackerForOverallCardSetHandlers = new ProgressTracker();
        _progressTrackerForFolderPreparer = new ProgressTracker();
        _pathSettings = pathSettings;
        _overrideData = overrideData;
    }

    public async Task PerformProcessingUsingHandlerAsync(ICardSetHandler handler)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var preparer = new DestinationFolderPreparer(_pathSettings);

            _progressTrackerForFolderPreparer.Total = preparer.GatherAllSourceSetFolders().Count;

            HashSet<CardSet> allCardSets;

            using (_progressBarForFolderPreparer = new ProgressBar(_progressTrackerForFolderPreparer.Total, "Folder Preparation (Number of Sets)"))
            {
                EventBroker.FolderPreparationProgressUpdated += OnFolderPreparerProgressUpdated;
                var allCardSeries = await preparer.SetupDestinationFoldersAsync(_cancellationToken);
                allCardSets = allCardSeries.SelectMany(series => series.Sets ?? []).ToHashSet();
                _progressTrackerForOverallCardSetHandlers.Total = GetAbsoluteTotalNumberOfImagesOnDiskAtSource(allCardSets);
            }

            using (_progressBarForCardSetHandlers = new ProgressBar(_progressTrackerForOverallCardSetHandlers.Total, "Card Processing (Number of Cards)"))
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
                        var dataOverride = _overrideData?.GetValueOrDefault(set.Tier)?.GetValueOrDefault(set.SeriesId)?.GetValueOrDefault(set.Id);
                        if (dataOverride != null)
                        {
                            var isOverwritten = set.OverwriteWith(dataOverride);
                            if (isOverwritten) Log.Information("Overwrote set {SetId} with override data", set.Id);
                        }
                        
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
                .EnumerateDirectories(_pathSettings.OutputFolderPath, "*", SearchOption.AllDirectories)
                .Where(dir => !Directory.EnumerateDirectories(dir).Any())
                .ToList();

            var compilationProgressTracker = new ProgressTracker
            {
                Total = allLeafFolders.Count
            };

            using var compileProgressBar = new ProgressBar(compilationProgressTracker.Total, "Compiling Master Metadata (setCount)");

            var tierDirectories = Directory.EnumerateDirectories(_pathSettings.OutputFolderPath, "*", SearchOption.TopDirectoryOnly).ToList();
            foreach (var tierFolder in tierDirectories)
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

                if (!completeMetadata.TryGetValue(series.Tier, out var cardSeries)) completeMetadata[series.Tier] = [];

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

            await JsonFileWriter.WriteToJsonAsync(completeMetadata, _pathSettings.MasterMetadataFilePath, _cancellationToken);
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
            foreach (var seriesFolder in Directory.EnumerateDirectories(_pathSettings.OutputFolderPath, "*", SearchOption.AllDirectories))
            foreach (var setFolder in Directory.EnumerateDirectories(seriesFolder, "*", SearchOption.TopDirectoryOnly))
            {
                var setMetadataFile = Path.Combine(setFolder, PathSettings.DefaultFilenameForSetMetadataJson);
                if (!File.Exists(setMetadataFile)) continue;

                var cardSet = await JsonFileReader.ReadFromJsonAsync<CardSet>(setMetadataFile, _cancellationToken);
                if (cardSet.Cards is null) continue;

                var destinations = cardSet.Cards.Select(card => card.DestinationAbsoluteFilePath).ToHashSet();
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
        progressBar.Tick(
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

    public static int GetMaximumNumberOfCardsToInclude(int totalNumberOfCards, GeneralSettings general, CardSetHandlerOverrideData? overrideData = null)
    {
        if (totalNumberOfCards < 0) return 0;
        
        if (overrideData?.IgnoreMaximumNumberOfCardsToIncludeLimit == true) return totalNumberOfCards;

        var calculatedMaximumNumberOfCards = (int)Math.Ceiling(totalNumberOfCards * general.MaximumImageSelectionPercentageForSet);
        return calculatedMaximumNumberOfCards <= general.MinimumImageSelectionCountForSet
            ? general.MinimumImageSelectionCountForSet
            : calculatedMaximumNumberOfCards;
    }
}