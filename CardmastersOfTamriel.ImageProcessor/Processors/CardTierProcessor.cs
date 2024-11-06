using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Processors;

public class CardTierProcessor
{
    private readonly Config _config;
    private readonly MasterMetadataHandler _metadataHandler;

    public CardTierProcessor(Config config, MasterMetadataHandler metadataHandler)
    {
        _config = config;
        _metadataHandler = metadataHandler;
    }

    public void ProcessTierFolder(string tierSourceFolderPath, string tierDestinationFolderPath, ICardSetProcessor cardSetProcessor)
    {
        Log.Information($"Processing Source Tier folder: '{tierSourceFolderPath}'");

        FileOperations.EnsureDirectoryExists(tierDestinationFolderPath);

        var processor = new CardSeriesProcessor(_config, _metadataHandler);
        var cardTier = Enum.Parse<CardTier>(Path.GetFileName(tierSourceFolderPath));
        foreach (var seriesSourceFolderPath in Directory.EnumerateDirectories(tierSourceFolderPath))
        {
            processor.ProcessSeriesFolder(cardTier, seriesSourceFolderPath, tierDestinationFolderPath, cardSetProcessor);
        }
    }
}