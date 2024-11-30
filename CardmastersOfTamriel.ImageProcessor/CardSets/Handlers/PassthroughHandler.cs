using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

internal class PassthroughHandler : ICardSetHandler
{
    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken,
        CardSetHandlerOverrideData? overrideData = null)
    {
        if (set.Cards != null)
            foreach (var card in set.Cards)
                EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(card));
    }
}