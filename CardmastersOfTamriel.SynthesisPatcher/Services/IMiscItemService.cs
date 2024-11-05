using Mutagen.Bethesda.Skyrim;
using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.SynthesisPatcher;

public interface IMiscItemService
{
    Dictionary<Card, MiscItem> InsertAndMapCardsToMiscItems(IEnumerable<Card> cards);
}