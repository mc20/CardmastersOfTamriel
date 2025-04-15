using System.Text.Json;
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
    private readonly Config _config;
    private HashSet<CardSetHandlerOverrideData>? _overrideData;

    private readonly ProgressTracker _progressTrackerForFolderPreparer;
    private readonly ProgressTracker _progressTrackerForOverallCardSetHandlers;

    private ProgressBar? _progressBarForCardSetHandlers;
    private ProgressBar? _progressBarForFolderPreparer;

    public ImageProcessingCoordinator(Config config, CancellationToken cancellationToken, HashSet<CardSetHandlerOverrideData>? overrideData = null)
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

        Log.Debug("Performing processing using handler {Handler}", handler.GetType().Name);

        try
        {
            FileOperations.EnsureDirectoryExists(_config.Paths.OutputFolderPath);
            
            var preparer = new DestinationFolderPreparer(_config.Paths);

            _progressTrackerForFolderPreparer.Total = preparer.GatherAllSourceSetFolders().Count;

            MasterMetadata metadata;
            var allCardSets = new HashSet<CardSet>();

            using (_progressBarForFolderPreparer = new ProgressBar(_progressTrackerForFolderPreparer.Total,
                       $"Folder Preparation (Number of Sets: {_progressTrackerForFolderPreparer.Total})"))
            {
                EventBroker.FolderPreparationProgressUpdated += OnFolderPreparerProgressUpdated;

                metadata = await preparer.SetupDestinationFoldersAsync(_cancellationToken);
                allCardSets.UnionWith(metadata.Metadata.Values.SelectMany(series => series.SelectMany(s => s.Sets ?? [])).ToHashSet());
                _progressTrackerForOverallCardSetHandlers.Total = GetAbsoluteTotalNumberOfImagesOnDiskAtSource(allCardSets);
            }

            if (_overrideData == null)
            {
                var helper = new CardOverrideDataHelper(_config, _cancellationToken);
                _overrideData = await helper.CreateNewOverrideDataFromCardSets(metadata);
                await JsonFileWriter.WriteToJsonAsync(_overrideData, _config.Paths.SetMetadataOverrideFilePath, _cancellationToken);
            }

            using (_progressBarForCardSetHandlers = new ProgressBar(_progressTrackerForOverallCardSetHandlers.Total, "Card Processing (Number of Cards)"))
            {
                await HandleCardSetsAsync(handler, allCardSets);
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

    private async Task HandleCardSetsAsync(ICardSetHandler handler, HashSet<CardSet> allCardSets)
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
                var dataOverride = _overrideData?.FirstOrDefault(d => d.CardSetId == set.Id);
                Log.Debug("Processing set {SetId} with override data: {DataOverride}", set.Id,
                    JsonSerializer.Serialize(dataOverride, JsonSettings.Options));
                if (dataOverride != null)
                {
                    var isOverwritten = set.OverwriteWith(dataOverride);
                    if (isOverwritten) Log.Information("Overwrote set {SetId} with override data", set.Id);
                }

                Log.Debug("Processing set {SetId}", set.Id);
                await handler.ProcessCardSetAsync(set, token, dataOverride);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while processing series");
                throw;
            }
        });
    }

    private void OnCardSetHandlerProgressUpdated(object? sender, ProgressTrackingEventArgs? e)
    {
        if (e == null) return;

        if (_progressBarForCardSetHandlers is not null)
            ProgressTrackerUpdater.UpdateProgressTracker(_progressTrackerForOverallCardSetHandlers, _progressBarForCardSetHandlers, "Processing all Cards");
    }

    private void OnFolderPreparerProgressUpdated(object? sender, ProgressTrackingEventArgs? e)
    {
        if (e == null) return;

        if (_progressBarForFolderPreparer is not null)
            ProgressTrackerUpdater.UpdateProgressTracker(_progressTrackerForFolderPreparer, _progressBarForFolderPreparer, "Processing all set folders");
    }

    private static int GetAbsoluteTotalNumberOfImagesOnDiskAtSource(HashSet<CardSet> allCardSets)
    {
        return allCardSets.Sum(set =>
            ImageFilePathUtility.GetImageFilePathsFromFolder(set.SourceAbsoluteFolderPath).Count);
    }

    public static int GetMaximumNumberOfCardsToInclude(int totalNumberOfCards, GeneralSettings general, CardSetHandlerOverrideData? overrideData = null)
    {
        Log.Debug(
            $"Calculating maximum number of cards to include for {totalNumberOfCards} cards with override IgnoreMaximumNumberOfCardsToIncludeLimit: {overrideData?.IgnoreMaximumNumberOfCardsToIncludeLimit}");
        if (totalNumberOfCards < 0) return 0;

        if (overrideData?.IgnoreMaximumNumberOfCardsToIncludeLimit == true) return totalNumberOfCards;

        var calculatedMaximumNumberOfCards = Math.Min((int)Math.Ceiling(totalNumberOfCards * general.MaximumImageSelectionPercentageForSet), general.MaximumImageSelectionCountForSet);
        
        return calculatedMaximumNumberOfCards <= general.MinimumImageSelectionCountForSet
            ? general.MinimumImageSelectionCountForSet
            : calculatedMaximumNumberOfCards;
    }
}