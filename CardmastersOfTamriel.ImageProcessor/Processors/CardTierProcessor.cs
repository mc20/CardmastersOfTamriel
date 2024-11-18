using CardmastersOfTamriel.ImageProcessor.CardSets;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Processors;

public static class CardTierProcessor
{

    [Obsolete("Use ProcessTierFolderAsync instead", false)]
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

    public static async Task ProcessTierFolderAsync(string tierSourceFolderPath, string tierDestinationFolderPath, IAsyncCardSetHandler asyncCardSetHandler, CancellationToken cancellationToken)
    {
        Log.Information($"Processing Source Tier folder: '{tierSourceFolderPath}'");

        FileOperations.EnsureDirectoryExists(tierDestinationFolderPath);

        var processor = await CardSeriesProcessor.CreateAsync(cancellationToken);

        var cardTier = Enum.Parse<CardTier>(Path.GetFileName(tierSourceFolderPath));

        foreach (var seriesSourceFolderPath in Directory.EnumerateDirectories(tierSourceFolderPath).Order())
        {
            await processor.ProcessSeriesFolderAsync(cardTier, seriesSourceFolderPath, tierDestinationFolderPath, asyncCardSetHandler, cancellationToken);
        }
    }
}