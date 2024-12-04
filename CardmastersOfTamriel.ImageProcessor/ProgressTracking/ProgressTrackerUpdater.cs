using CardmastersOfTamriel.Utilities;
using ShellProgressBar;

namespace CardmastersOfTamriel.ImageProcessor.ProgressTracking;

public class ProgressTrackerUpdater
{
    public static void UpdateProgressTracker(ProgressTracker tracker, ProgressBar progressBar, string message = "")
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
}