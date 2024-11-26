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
}

public static class CardSeriesExtensionMethods
{
    public static void OverrideWith(this CardSeries series, CardSeriesBasicMetadata seriesOverride)
    {
        series.DisplayName = seriesOverride.DisplayName;
        series.Description = seriesOverride.Description;
        series.DefaultKeywords = seriesOverride.DefaultKeywords;
    }
}

public static class CardSetExtensionMethods
{
    public static void OverrideWith(this CardSet set, CardSetBasicMetadata setOverride)
    {
        set.DisplayName = setOverride.DisplayName;
        set.DefaultValue = setOverride.DefaultValue;
        set.DefaultWeight = setOverride.DefaultWeight;
        set.DefaultKeywords = setOverride.DefaultKeywords;
    }
}