using CardmastersOfTamriel.ImageProcessor.CardSets;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Processors;

public static class CardTierProcessor
{
    public static async Task ProcessTierFolderAsync(string tierSourceFolderPath, string tierDestinationFolderPath, ICardSetHandler asyncCardSetHandler, CancellationToken cancellationToken)
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
                await CardSeriesProcessor.ProcessSeriesFolderAsync(cardTier, seriesSourceFolderPath, tierDestinationFolderPath, asyncCardSetHandler, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error processing series folder: '{seriesSourceFolderPath}'");
            }
        });
    }
}