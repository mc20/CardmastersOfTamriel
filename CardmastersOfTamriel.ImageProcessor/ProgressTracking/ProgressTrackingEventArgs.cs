namespace CardmastersOfTamriel.ImageProcessor.ProgressTracking;

public class ProgressTrackingEventArgs(string setId) : EventArgs
{
    public string SetId { get; } = setId;
}