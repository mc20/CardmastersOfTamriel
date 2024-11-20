using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.ProgressTracking;

public static class EventBroker
{
    public static event EventHandler<ProgressTrackingEventArgs>? SetHandlingProgressUpdated;
    public static event EventHandler<ProgressTrackingEventArgs>? FolderPreparationProgressUpdated;

    public static void PublishSetHandlingProgress(object? sender, ProgressTrackingEventArgs e)
    {
        Log.Information($"PublishSetProgress updated for {e.SetId}");
        SetHandlingProgressUpdated?.Invoke(sender, e);
    }

    public static void PublishFolderPreparationProgress(object? sender, ProgressTrackingEventArgs e)
    {
        Log.Information($"PublishFolderPreparationProgress updated for {e.SetId}");
        FolderPreparationProgressUpdated?.Invoke(sender, e);
    }
}