using System.Diagnostics;

namespace CardmastersOfTamriel.ImageProcessor.ProgressTracking;

public class ProgressTracker
{
    public readonly Stopwatch ProgressStopwatch = new();
    public int Current { get; set; }
    public int Total { get; set; }
    public int RemainingCount => Total - Current;
    public bool IsComplete => Current >= Total;

    public double CalculateEstimatedTimeLeft()
    {
        var averageTimePerFile = ProgressStopwatch.Elapsed.TotalMilliseconds / Current;
        return averageTimePerFile * RemainingCount;
    }
}
