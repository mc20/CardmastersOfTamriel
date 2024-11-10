using System.Text.Json;
using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets;

public class CardSetReportProcessor : ICardSetHandler
{
    public void ProcessCardSet(CardSet set)
    {
        var savedJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "cards.jsonl");
        var savedCards = LoadCardsFromJsonFile(savedJsonFilePath);

        var imagesAtSource = CardSetImageHelper.GetImageFilePathsFromFolder(set.SourceAbsoluteFolderPath);
        var imagesAtDestination =
            CardSetImageHelper.GetImageFilePathsFromFolder(set.DestinationAbsoluteFolderPath, ["*.dds"]);

        CardSetReportProvider.Instance.UpdateWithSetInfo(set, savedCards, imagesAtDestination.Count,
            imagesAtSource.Count);
    }

    private static List<Card> LoadCardsFromJsonFile(string savedJsonFilePath)
    {
        if (!File.Exists(savedJsonFilePath))
        {
            Log.Warning($"No saved cards found at path: '{savedJsonFilePath}'");
            return [];
        }

        var lines = new HashSet<string>(File.ReadLines(savedJsonFilePath));
        var uniqueCards = new HashSet<string>();

        var cardsFromMetadataFile = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<Card>(line, JsonSettings.Options))
            .Where(card => card != null && uniqueCards.Add(card.Id!))
            .Select(card => card!)
            .ToList();

        return cardsFromMetadataFile;
    }
}