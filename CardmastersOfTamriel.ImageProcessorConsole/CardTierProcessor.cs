using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public class CardTierProcessor
{
    private readonly AppConfig _appConfig;
    private readonly ImageHelper _imageHelper;
    private readonly CardSeriesProcessor _seriesProcessor;

    public CardTierProcessor(AppConfig appConfig, ImageHelper imageHelper)
    {
        _appConfig = appConfig;
        _imageHelper = imageHelper;
        _seriesProcessor = new CardSeriesProcessor(_appConfig, _imageHelper);
    }

    public void ProcessTierFolder(string tierSourceFolderPath, string tierDestinationFolderPath)
    {
        Logger.LogAction($"Processing Source Tier folder: '{tierSourceFolderPath}'");

        FileOperations.EnsureDirectoryExists(tierDestinationFolderPath);

        var cardTier = Enum.Parse<CardTier>(Path.GetFileName(tierSourceFolderPath));
        foreach (var seriesSourceFolderPath in Directory.EnumerateDirectories(tierSourceFolderPath))
        {
            _seriesProcessor.ProcessSeriesFolder(cardTier, seriesSourceFolderPath, tierDestinationFolderPath);
        }
    }
}
