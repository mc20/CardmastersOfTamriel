using System.Text.Json;
using CardmastersOfTamriel.ImageProcessor.Processors;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public class CardSetImageSizeProcessor : ICardSetProcessor
{
    private readonly Config _config;

    public CardSetImageSizeProcessor(Config config)
    {
        _config = config;
    }

    public void ProcessSetAndImages(CardSet set)
    {
        var savedJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "source_images_metadata.json");

        var data = new Dictionary<string, CardShape>();
        var imageFilePaths = CardSetImageHelper.GetImageFilePathsFromFolder(set.SourceAbsoluteFolderPath);

        foreach (var imageFilePath in imageFilePaths)
        {
            var imageShape = ImageHelper.DetermineOptimalShape(_config, imageFilePath);
            data.Add(imageFilePath, imageShape);
        }

        var serializedJson = JsonSerializer.Serialize(data, JsonSettings.Options);
        File.WriteAllText(savedJsonFilePath, serializedJson);
        Log.Information($"SAVING Metadata for Source Image Folder to {savedJsonFilePath}");
    }
}