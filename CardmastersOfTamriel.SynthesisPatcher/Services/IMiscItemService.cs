using CardmastersOfTamriel.Models;
using Mutagen.Bethesda.Skyrim;

namespace CardmastersOfTamriel.SynthesisPatcher.Services;

public interface IMiscItemService
{
    Dictionary<Card, MiscItem> InsertAndMapCardsToMiscItems(IEnumerable<Card> cards);
}