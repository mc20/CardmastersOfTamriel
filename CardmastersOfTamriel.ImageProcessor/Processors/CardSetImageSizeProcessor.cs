using System.Text.Json;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Processors;

public class CardSetImageSizeProcessor : ICardSetProcessor
{
    public void ProcessSetAndImages(CardSet set)
    {
        Log.Information($"Processing Set from Source Path: '{set.SourceAbsoluteFolderPath}'");

        set.Cards ??= [];
        set.Cards.Clear();

        var savedJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "cards.jsonl");
        var savedJsonBackupFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "cards.jsonl.backup");

        var cardsFromMetadataFile = LoadCardsFromJsonFile(savedJsonFilePath, savedJsonBackupFilePath);

        // var savedJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "source_images_metadata.json");
        //
        // var data = new Dictionary<string, CardShape>();
        // var imageFilePaths = CardSetImageHelper.GetImageFilePathsFromFolder(set.SourceAbsoluteFolderPath);
        //
        // foreach (var imageFilePath in imageFilePaths.Order())
        // {
        //     var imageShape = ImageHelper.DetermineOptimalShape(imageFilePath);
        //     data.Add(imageFilePath, imageShape);
        // }
        //
        // var serializedJson = JsonSerializer.Serialize(data, JsonSettings.Options);
        // File.WriteAllText(savedJsonFilePath, serializedJson);
        // Log.Information($"SAVING Metadata for Source Image Folder to {savedJsonFilePath}");
    }

    private static List<Card> LoadCardsFromJsonFile(string savedJsonFilePath, string savedJsonBackupFilePath)
    {
        // if (!File.Exists(savedJsonFilePath)) return [];
        //
        // if (File.Exists(savedJsonBackupFilePath))
        // {
        //     File.Delete(savedJsonBackupFilePath);
        // }
        //
        // File.Copy(savedJsonFilePath, savedJsonBackupFilePath);

        var lines = new HashSet<string>(File.ReadLines(savedJsonFilePath));
        var uniqueCards = new HashSet<string>();

        var cardsFromMetadataFile = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<Card>(line, JsonSettings.Options))
            .Where(card => card != null && uniqueCards.Add(card.Id!))
            .Select(card => card!)
            .ToList();

        // File.Delete(savedJsonFilePath);

        return cardsFromMetadataFile;
    }
}