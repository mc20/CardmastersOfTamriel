using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.ProgressTracking;

public static class EventBroker
{
    public static event EventHandler<ProgressTrackingEventArgs>? SetHandlingProgressUpdated;
    public static event EventHandler<ProgressTrackingEventArgs>? FolderPreparationProgressUpdated;
    public static event EventHandler<ProgressTrackingEventArgs>? PublishMetadataCompilationProgressUpdated;

    public static void PublishSetHandlingProgress(object? sender, ProgressTrackingEventArgs e)
    {
        Log.Debug($"[EVENT] Updated card: Series '{e.SeriesId}' Set '{e.SetId}' Card '{e.CardId}'");
        SetHandlingProgressUpdated?.Invoke(sender, e);
    }

    public static void PublishFolderPreparationProgress(object? sender, ProgressTrackingEventArgs e)
    {
        Log.Debug($"[EVENT] Updated series folder: Series '{e.SeriesId}' Set '{e.SetId}'");
        FolderPreparationProgressUpdated?.Invoke(sender, e);
    }
    public static void PublishMetadataCompilationProgress(object? sender, ProgressTrackingEventArgs e)
    {
        Log.Debug($"[EVENT] Updated series folder: Series '{e.SeriesId}' Set '{e.SetId}'");
        PublishMetadataCompilationProgressUpdated?.Invoke(sender, e);
    }
}