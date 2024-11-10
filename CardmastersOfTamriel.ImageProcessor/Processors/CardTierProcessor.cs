using CardmastersOfTamriel.ImageProcessor.CardSets;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Processors;

public static class CardTierProcessor
{
    public static void ProcessTierFolder(string tierSourceFolderPath, string tierDestinationFolderPath, ICardSetHandler cardSetHandler)
    {
        Log.Information($"Processing Source Tier folder: '{tierSourceFolderPath}'");

        FileOperations.EnsureDirectoryExists(tierDestinationFolderPath);

        var processor = new CardSeriesProcessor();

        var cardTier = Enum.Parse<CardTier>(Path.GetFileName(tierSourceFolderPath));

        foreach (var seriesSourceFolderPath in Directory.EnumerateDirectories(tierSourceFolderPath).Order())
        {
            processor.ProcessSeriesFolder(cardTier, seriesSourceFolderPath, tierDestinationFolderPath, cardSetHandler);
        }
    }
}