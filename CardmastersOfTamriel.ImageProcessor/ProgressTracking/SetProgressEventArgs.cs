namespace CardmastersOfTamriel.ImageProcessor.ProgressTracking;

public class SetProgressEventArgs(string setId) : EventArgs
{
    public string SetId { get; } = setId;
}