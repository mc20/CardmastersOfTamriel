using CardmastersOfTamriel.ImageProcessor.CardSets;
using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.ImageProcessor.Setup;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;
using ShellProgressBar;

namespace CardmastersOfTamriel.ImageProcessor;

public class ImageProcessingCoordinator
{
    private ProgressBar? _progressBarForCardSetHandlers;
    private ProgressBar? _progressBarForFolderPreparer;
    private readonly ProgressTracker _progressTrackerForOverallCardSetHandlers;
    private readonly ProgressTracker _progressTrackerForFolderPreparer;

    public ImageProcessingCoordinator()
    {
        _progressTrackerForOverallCardSetHandlers = new ProgressTracker();
        _progressTrackerForFolderPreparer = new ProgressTracker();
    }

    public async Task PerformProcessingUsingHandlerAsync(ICardSetHandler handler, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _progressTrackerForFolderPreparer.Total = DestinationFolderPreparer.GetTotalFolderCountAtSource();

            HashSet<CardSet> allCardSets;

            using (_progressBarForFolderPreparer = new ProgressBar(_progressTrackerForFolderPreparer.Total, "Folder Preparation (setCount)"))
            {
                EventBroker.FolderPreparationProgressUpdated += OnFolderPreparerProgressUpdated;
                var allCardSeries = await DestinationFolderPreparer.SetupDestinationFoldersAsync(cancellationToken);
                allCardSets = allCardSeries.SelectMany(series => series.Sets ?? []).ToHashSet();
                _progressTrackerForOverallCardSetHandlers.Total = GetAbsoluteTotalNumberOfCards(allCardSets);
            }

            using (_progressBarForCardSetHandlers = new ProgressBar(_progressTrackerForOverallCardSetHandlers.Total, "Overall Progress"))
            {
                EventBroker.SetHandlingProgressUpdated += OnCardSetHandlerProgressUpdated;

                await Parallel.ForEachAsync(allCardSets, new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = cancellationToken
                }, async (set, token) =>
                {
                    try
                    {
                        await handler.ProcessCardSetAsync(set, token);
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
            // handler.ProgressUpdated -= OnCardSetHandlerProgressUpdated;
            EventBroker.SetHandlingProgressUpdated -= OnCardSetHandlerProgressUpdated;
            EventBroker.FolderPreparationProgressUpdated -= OnFolderPreparerProgressUpdated;
        }

        await CompileSeriesMetadataAsync(cancellationToken);
        await CleanupNonTrackedFilesAtDestination(cancellationToken);
    }

    private static async Task CompileSeriesMetadataAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var completeMetadata = new Dictionary<CardTier, HashSet<CardSeries>>();

            var outputFolderPath = ConfigurationProvider.Instance.Config.Paths.OutputFolderPath;
            foreach (var seriesFolder in Directory.EnumerateDirectories(outputFolderPath, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var seriesMetadataFile = Path.Combine(seriesFolder, "series_metadata.json");
                if (!File.Exists(seriesMetadataFile)) continue;

                var series = await JsonFileReader.ReadFromJsonAsync<CardSeries>(seriesMetadataFile, cancellationToken);

                if (!completeMetadata.TryGetValue(series.Tier, out var cardSeries))
                {
                    completeMetadata[series.Tier] = [];
                }
                completeMetadata[series.Tier].Add(series);

                foreach (var setFolder in Directory.EnumerateDirectories(seriesFolder, "*", SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var setMetadataFile = Path.Combine(setFolder, "set_metadata.json");
                    if (!File.Exists(setMetadataFile)) continue;

                    var set = await JsonFileReader.ReadFromJsonAsync<CardSet>(setMetadataFile, cancellationToken);
                    series.Sets ??= [];
                    series.Sets.Add(set);
                }
            }

            await JsonFileWriter.WriteToJsonAsync(completeMetadata,
                ConfigurationProvider.Instance.Config.Paths.MasterMetadataFilePath, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while compiling series metadata");
            throw;
        }
    }

    private static async Task CleanupNonTrackedFilesAtDestination(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var outputFolderPath = ConfigurationProvider.Instance.Config.Paths.OutputFolderPath;

            foreach (var seriesFolder in Directory.EnumerateDirectories(outputFolderPath, "*", SearchOption.AllDirectories))
            {
                foreach (var setFolder in Directory.EnumerateDirectories(seriesFolder, "*", SearchOption.TopDirectoryOnly))
                {
                    var setMetadataFile = Path.Combine(setFolder, "cards.jsonl");
                    if (!File.Exists(setMetadataFile)) continue;

                    var cards = await JsonFileReader.LoadAllFromJsonLineFileAsync<Card>(setMetadataFile, cancellationToken);

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
            Console.WriteLine(e);
            throw;
        }
    }

    private void OnCardSetHandlerProgressUpdated(object? sender, ProgressTrackingEventArgs? e)
    {
        if (e == null) return;

        // Start the stopwatch on the first update
        if (_progressTrackerForOverallCardSetHandlers.Current == 0)
            _progressTrackerForOverallCardSetHandlers.ProgressStopwatch.Start();

        _progressTrackerForOverallCardSetHandlers.Current++;

        var estimatedTimeLeft = _progressTrackerForOverallCardSetHandlers.CalculateEstimatedTimeLeft();

        // Update the progress bar with the corrected ETA
        _progressBarForCardSetHandlers?.Tick(
            $"Processing all cards: {_progressTrackerForOverallCardSetHandlers.Current}/{_progressTrackerForOverallCardSetHandlers.Total} [ETA: {NameHelper.FormatDuration((long)estimatedTimeLeft)}]"
        );

        // Stop the stopwatch if processing is complete
        if (_progressTrackerForOverallCardSetHandlers.IsComplete)
            _progressTrackerForOverallCardSetHandlers.ProgressStopwatch.Stop();
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
            $"Processing all set folders: {_progressTrackerForFolderPreparer.Current}/{_progressTrackerForFolderPreparer.Total} [ETA: {NameHelper.FormatDuration((long)estimatedTimeLeft)}]"
        );

        // Stop the stopwatch if processing is complete
        if (_progressTrackerForFolderPreparer.IsComplete)
            _progressTrackerForFolderPreparer.ProgressStopwatch.Stop();
    }

    private static int GetAbsoluteTotalNumberOfCards(HashSet<CardSet> allCardSets)
    {
        return allCardSets.Sum(set => ImageFilePathUtility.GetImageFilePathsFromFolder(set.SourceAbsoluteFolderPath).Count);
    }
}