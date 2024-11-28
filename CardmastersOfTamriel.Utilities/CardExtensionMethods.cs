using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.Utilities;

public static class CardExtensionMethods
{
    public static HashSet<Card> ConsolidateCardsWith(this IEnumerable<Card> list1, IEnumerable<Card> list2)
    {
        var cardMap = new Dictionary<string, Card>();

        foreach (var card in list1)
        {
            cardMap[card.Id] = card;
        }

        foreach (var card in list2)
        {
            if (cardMap.TryGetValue(card.Id, out var existingCard))
            {
                // Update only if DestinationAbsoluteFilePath is empty or null in the existing card.
                if (string.IsNullOrEmpty(existingCard.DestinationAbsoluteFilePath))
                {
                    cardMap[card.Id] = card;
                }
            }
            else
            {
                cardMap[card.Id] = card;
            }
        }

        return cardMap.Values.ToHashSet();
    }

    /// <summary>
    /// Overwrites the properties of the card with the provided override data values if override data is not null.
    /// </summary>
    /// <param name="card">The card to be overwritten.</param>
    /// <param name="data">The data containing the values to overwrite the card's properties.</param>
    public static bool OverwriteWith(this Card card, CardOverrideData data)
    {
        var isOverwritten = false;
        if (data.ValueToOverwriteEachCardValue.HasValue && card.Value != data.ValueToOverwriteEachCardValue.Value)
        {
            card.Value = data.ValueToOverwriteEachCardValue.Value;
            isOverwritten = true;
        }

        if (data.ValueToOverwriteEachCardWeight.HasValue && card.Weight != data.ValueToOverwriteEachCardWeight.Value)
        {
            card.Weight = data.ValueToOverwriteEachCardWeight.Value;
            isOverwritten = true;
        }
        
        if (data.KeywordsToOverwriteEachCardKeywords != null && card.Keywords is not null)
        {
            var differences = data.KeywordsToOverwriteEachCardKeywords.Except(card.Keywords).Union(card.Keywords.Except(data.KeywordsToOverwriteEachCardKeywords));
            if (differences.Any())
            {
                card.Keywords = data.KeywordsToOverwriteEachCardKeywords.ToHashSet();
                isOverwritten = true;
            }
        }

        return isOverwritten;
    }
}