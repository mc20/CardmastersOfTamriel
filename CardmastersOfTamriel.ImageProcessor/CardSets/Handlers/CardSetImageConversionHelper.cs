﻿using CardmastersOfTamriel.ImageProcessor.Models;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public static class CardSetImageConversionHelper
{
    public static async Task ProcessAndUpdateCardForConversion(CardSet set, Card card, int index, int trueTotalCount,
        CancellationToken cancellationToken)
    {
        var result = await ConvertAndSaveImageAsync(set, card.SourceAbsoluteFilePath ?? string.Empty,
            NameHelper.CreateImageFileName(set, (uint)index + 1), cancellationToken);

        card.ConversionDate = DateTime.UtcNow;
        card.Shape = result.Shape;
        card.DisplayName = null;
        card.DestinationAbsoluteFilePath = result.DestinationAbsoluteFilePath;
        card.DestinationRelativeFilePath =
            FilePathHelper.GetRelativePath(result.DestinationAbsoluteFilePath, set.Tier);
        card.DisplayedIndex = 0;
        card.DisplayedTotalCount = 0;
        card.TrueIndex = (uint)index + 1;
        card.TrueTotalCount = (uint)trueTotalCount;
        card.SetGenericDisplayName();
    }

    public static void UpdateUnconvertedCard(Card card, uint index, uint totalTrueCount)
    {
        Log.Verbose($"Card {card.Id} was not converted and will be skipped");
        card.Shape =
            ImageHelper.DetermineOptimalShape(card
                .SourceAbsoluteFilePath!); // Keep track of the shape for future reference
        card.DisplayName = null;
        card.DestinationAbsoluteFilePath = null;
        card.DestinationRelativeFilePath = null;
        card.DisplayedIndex = 0;
        card.DisplayedTotalCount = 0;
        card.TrueIndex = index + 1;
        card.TrueTotalCount = totalTrueCount;
    }

    public static void UpdateDisplayCards(List<Card> finalCards)
    {
        var cardsEligibleForDisplay =
            finalCards.Where(card => !string.IsNullOrWhiteSpace(card.DestinationAbsoluteFilePath)).ToList();
        foreach (var cardInfo in cardsEligibleForDisplay.OrderBy(card => card.Id)
                     .Select((card, index) => (card, index)))
        {
            cardInfo.card.DisplayedIndex = (uint)cardInfo.index + 1;
            cardInfo.card.DisplayedTotalCount = (uint)cardsEligibleForDisplay.Count;
            cardInfo.card.SetGenericDisplayName();
        }
    }

    private static async Task<ConversionResult> ConvertAndSaveImageAsync(CardSet set, string sourceImageFilePath,
        string imageFileName, CancellationToken cancellationToken)
    {
        var imageDestinationFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, imageFileName);

        var helper = new ImageConverter();
        var imageShape = await helper.ConvertImageAndSaveToDestinationAsync(set.Tier, sourceImageFilePath,
            imageDestinationFilePath, cancellationToken);

        return new ConversionResult()
        {
            Shape = imageShape,
            DestinationAbsoluteFilePath = imageDestinationFilePath
        };
    }
}