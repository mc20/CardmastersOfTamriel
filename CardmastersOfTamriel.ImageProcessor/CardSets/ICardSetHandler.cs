using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessor.CardSets;

public interface ICardSetHandler
{
    Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken, CardOverrideData? overrideData = null);
}
