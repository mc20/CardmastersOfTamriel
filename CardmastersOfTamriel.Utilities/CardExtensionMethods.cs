namespace CardmastersOfTamriel.Utilities;

public static class CardExtensionMethods
{
    public static HashSet<Card> ConsolidateCardsWith(this IEnumerable<Card> list1, IEnumerable<Card> list2)
    {
        var cardMap = new Dictionary<string, Card>();
        foreach (var card in list1.Where(card => card.Id != null))
        {
            cardMap[card.Id!] = card;
        }

        foreach (var card in list2.Where(card => card.Id != null))
        {
            if (cardMap.ContainsKey(card.Id!))
            {
                var existingCard = cardMap[card.Id!];
                if (string.IsNullOrEmpty(existingCard.DestinationAbsoluteFilePath))
                {
                    cardMap[card.Id!] = card;
                }
            }
            else
            {
                cardMap[card.Id!] = card;
            }
        }

        return [.. cardMap.Values];
    }
}