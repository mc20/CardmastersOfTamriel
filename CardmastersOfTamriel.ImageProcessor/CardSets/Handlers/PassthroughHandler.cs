using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

internal class PassthroughHandler : ICardSetHandler
{
    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken,
        CardOverrideData? overrideData = null)
    {
        if (set.Cards != null)
        {
            foreach (var card in set.Cards)
            {
                // if (overrideData is not null) card.OverwriteWith(overrideData);
                EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(card));
            }
        }
    }
}