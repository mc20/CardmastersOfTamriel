using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessor.CardSets;

public interface ICardSetHandler
{
    void ProcessCardSet(CardSet set);
}

public interface IAsyncCardSetHandler
{
    Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken);
}
