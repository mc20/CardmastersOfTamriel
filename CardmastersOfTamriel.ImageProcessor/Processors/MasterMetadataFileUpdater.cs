using System.Text.Json;
using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessor.Processors;

public class MasterMetadataFileUpdater : ICardSetProcessor
{
    public void ProcessSetAndImages(CardSet set)
    {
        var handler = MasterMetadataProvider.Instance.MetadataHandler;

        var savedJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "cards.jsonl");

        if (!File.Exists(savedJsonFilePath)) return;

        var lines = new HashSet<string>(File.ReadLines(savedJsonFilePath));
        var uniqueCards = new HashSet<string>();

        var cardsFromMetadataFile = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<Card>(line, JsonSettings.Options))
            .Where(card => card != null && uniqueCards.Add(card.Id!))
            .Select(card => card!)
            .ToList();

        var cardsMetadata = handler.Metadata.Series?.SelectMany(series => series.Sets ?? [])
            .FirstOrDefault(s => s.Id == set.Id);

        if (cardsMetadata == null) return;

        cardsMetadata.Cards = cardsFromMetadataFile.ToHashSet();

        handler.WriteMetadataToFile();
    }
}