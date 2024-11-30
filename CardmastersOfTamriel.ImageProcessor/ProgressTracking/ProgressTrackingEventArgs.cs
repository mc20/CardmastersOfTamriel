using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessor.ProgressTracking;

public class ProgressTrackingEventArgs : EventArgs
{
    public ProgressTrackingEventArgs(Card card)
    {
        SeriesId = card.SeriesId;
        SetId = card.SetId;
        CardId = card.Id;
    }

    public ProgressTrackingEventArgs(CardSet set)
    {
        SeriesId = set.SeriesId;
        SetId = set.Id;
        CardId = null;
    }

    public string? SeriesId { get; }
    public string? SetId { get; }
    public string? CardId { get; }
}