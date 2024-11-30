using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.Utilities;

public static class CardExtensionMethods
{
    public static bool OverwriteWith(this Card card, Card otherCard)
    {
        var isOverwritten = false;

        if (card.SetDisplayName != otherCard.SetDisplayName)
        {
            card.SetDisplayName = otherCard.SetDisplayName;
            isOverwritten = true;
        }

        if (card.SeriesId != otherCard.SeriesId)
        {
            card.SeriesId = otherCard.SeriesId;
            isOverwritten = true;
        }

        if (card.ConversionDate != otherCard.ConversionDate)
        {
            card.ConversionDate = otherCard.ConversionDate;
            isOverwritten = true;
        }

        if (card.Shape != otherCard.Shape)
        {
            card.Shape = otherCard.Shape;
            isOverwritten = true;
        }

        if (card.DisplayName != otherCard.DisplayName)
        {
            card.DisplayName = otherCard.DisplayName;
            isOverwritten = true;
        }

        if (card.DisplayedIndex != otherCard.DisplayedIndex)
        {
            card.DisplayedIndex = otherCard.DisplayedIndex;
            isOverwritten = true;
        }

        if (card.DisplayedTotalCount != otherCard.DisplayedTotalCount)
        {
            card.DisplayedTotalCount = otherCard.DisplayedTotalCount;
            isOverwritten = true;
        }

        if (card.TrueIndex != otherCard.TrueIndex)
        {
            card.TrueIndex = otherCard.TrueIndex;
            isOverwritten = true;
        }

        if (card.TrueTotalCount != otherCard.TrueTotalCount)
        {
            card.TrueTotalCount = otherCard.TrueTotalCount;
            isOverwritten = true;
        }

        if (card.Description != otherCard.Description)
        {
            card.Description = otherCard.Description;
            isOverwritten = true;
        }

        if (card.Tier != otherCard.Tier)
        {
            card.Tier = otherCard.Tier;
            isOverwritten = true;
        }

        if (card.Value != otherCard.Value)
        {
            card.Value = otherCard.Value;
            isOverwritten = true;
        }

        if (card.Weight != otherCard.Weight)
        {
            card.Weight = otherCard.Weight;
            isOverwritten = true;
        }

        if (card.Keywords == null || !card.Keywords.SetEquals(otherCard.Keywords ?? []))
        {
            card.Keywords = otherCard.Keywords;
            isOverwritten = true;
        }

        if (card.DestinationAbsoluteFilePath != otherCard.DestinationAbsoluteFilePath)
        {
            card.DestinationAbsoluteFilePath = otherCard.DestinationAbsoluteFilePath;
            isOverwritten = true;
        }

        if (card.DestinationRelativeFilePath != otherCard.DestinationRelativeFilePath)
        {
            card.DestinationRelativeFilePath = otherCard.DestinationRelativeFilePath;
            isOverwritten = true;
        }

        return isOverwritten;
    }

    /// <summary>
    ///     Overwrites the properties of the card with the provided override data values if override data is not null.
    /// </summary>
    /// <param name="card">The card to be overwritten.</param>
    /// <param name="data">The data containing the values to overwrite the card's properties.</param>
    public static bool OverwriteWith(this Card card, CardSetHandlerOverrideData data)
    {
        var isOverwritten = false;
        
        if (data.UseOriginalFileNamesAsDisplayNames)
        {
            card.DisplayName = Path.GetFileNameWithoutExtension(card.SourceAbsoluteFilePath);
            isOverwritten = true;
        }
        
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
            var differences = data.KeywordsToOverwriteEachCardKeywords.Except(card.Keywords)
                .Union(card.Keywords.Except(data.KeywordsToOverwriteEachCardKeywords));
            if (differences.Any())
            {
                card.Keywords = data.KeywordsToOverwriteEachCardKeywords.ToHashSet();
                isOverwritten = true;
            }
        }

        return isOverwritten;
    }
}