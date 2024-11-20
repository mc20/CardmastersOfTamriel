using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessor.CardSets;

public interface ICardSetHandler
{
    event EventHandler<SetProgressEventArgs>? ProgressUpdated;
    Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken);
}
