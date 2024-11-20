using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.ProgressTracking;

public static class EventBroker
{
    public static event EventHandler<SetProgressEventArgs>? ProgressUpdated;

    public static void PublishSetProgress(object? sender, SetProgressEventArgs e)
    {
        Log.Information($"Progress updated for {e.SetId}");
        ProgressUpdated?.Invoke(sender, e);
    }
}