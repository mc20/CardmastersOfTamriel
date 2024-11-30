using CardmastersOfTamriel.Models;
using Serilog;

namespace CardmastersOfTamriel.Utilities;

public static class CardHelper
{
    public static HashSet<Card> CreateConsolidatedCardList(IEnumerable<Card> list1, IEnumerable<Card> list2)
    {
        var cardMap = new Dictionary<string, Card>();

        foreach (var card in list1) cardMap[card.Id] = card;

        foreach (var card in list2)
            if (cardMap.TryGetValue(card.Id, out var existingCard))
            {
                if (string.IsNullOrWhiteSpace(existingCard.DestinationAbsoluteFilePath))
                {
                    cardMap[card.Id] = card;
                }
                else
                {
                    var overwriteOccurred = existingCard.OverwriteWith(card);
                    if (overwriteOccurred) Log.Debug("Overwrote card {CardId} with card from source folder", card.Id);
                }
            }
            else
            {
                cardMap[card.Id] = card;
            }

        return cardMap.Values.ToHashSet();
    }
}