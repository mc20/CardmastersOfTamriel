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

        var cardTier = Enum.Parse<CardTier>(Path.GetFileName(tierSourceFolderPath));

        var seriesSourceFolders = Directory.EnumerateDirectories(tierSourceFolderPath).Order();

        await Parallel.ForEachAsync(seriesSourceFolders, new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        }, async (seriesSourceFolderPath, cancellationToken) =>
        {
            try
            {
                await CardSeriesProcessorAsync.ProcessSeriesFolderAsync(cardTier, seriesSourceFolderPath, tierDestinationFolderPath, asyncCardSetHandler, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error processing series folder: '{seriesSourceFolderPath}'");
            }
        });
    }
}