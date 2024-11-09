using Mutagen.Bethesda.Skyrim;

namespace CardmastersOfTamriel.SynthesisPatcher.MiscItems;

public interface ICardMiscItemCreator
{
    Dictionary<Card, MiscItem> InsertAndMapCardsToMiscItems(HashSet<Card> cards);
}