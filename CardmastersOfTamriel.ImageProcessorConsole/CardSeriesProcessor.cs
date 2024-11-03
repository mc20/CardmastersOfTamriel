using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public class CardSeriesProcessor
{
    private readonly AppConfig _appConfig;
    private readonly ImageHelper _imageHelper;

    public CardSeriesProcessor(AppConfig appConfig, ImageHelper imageHelper)
    {
        _appConfig = appConfig;
        _imageHelper = imageHelper;
    }

    public void ProcessSeriesFolder(CardTier tier, string seriesSourceFolderPath, string tierDestinationFolderPath)
    {
        Logger.LogAction($"Examining Source Series folder: '{seriesSourceFolderPath}'");

        var seriesId = Path.GetFileName(seriesSourceFolderPath);
        var cardSeries = GetOrCreateCardSeries(tier, seriesId, seriesSourceFolderPath, tierDestinationFolderPath);

        var seriesDestinationFolderPath = Path.Combine(tierDestinationFolderPath, seriesId);
        FileOperations.EnsureDirectoryExists(seriesDestinationFolderPath);

        MasterMetadataHandler.Instance.WriteMetadataToFile(); // Save progress

        var groupedFolders = DetermineFolderGrouping(seriesSourceFolderPath);

        var mirroror = new CardSetMirrorer(cardSeries);
        mirroror.CreateSetsAtDestination(groupedFolders);

        ProcessCardSets();

        MasterMetadataHandler.Instance.WriteMetadataToFile();
    }

    private void ProcessCardSets()
    {
        if (MasterMetadataHandler.Instance.Metadata?.Series is not null)
        {
            Logger.LogAction("Master Metadata Series:");

            var cardSetProcessor = new CardSetProcessor(_imageHelper);

            foreach (var series in MasterMetadataHandler.Instance.Metadata.Series.Where(series => series.Sets is not null))
            {
                if (series.Sets is null || series.Sets.Count == 0) continue;

                foreach (var set in series.Sets)
                {
                    var id = set.Id ?? "";
                    Logger.LogAction($"Set Id: '{id}' => Destination Folder Path: '{set.DestinationFolderPath}'");

                    cardSetProcessor.ProcessSetAndImages(set);
                }
            }
        }
    }

    private static CardSeries GetOrCreateCardSeries(CardTier tier, string seriesId, string seriesSourceFolderPath, string tierDestinationFolderPath)
    {
        var cardSeries = MasterMetadataHandler.Instance.Metadata.Series?.FirstOrDefault(s => s.Id == seriesId);
        if (cardSeries != null)
        {
            cardSeries.Tier = tier;
            cardSeries.SourceFolderPath = seriesSourceFolderPath;
            cardSeries.DestinationFolderPath = Path.Combine(tierDestinationFolderPath, seriesId);
        }
        else
        {
            cardSeries = CardSeriesFactory.CreateNewSeries(seriesId, tier);
            cardSeries.SourceFolderPath = seriesSourceFolderPath;
            cardSeries.DestinationFolderPath = Path.Combine(tierDestinationFolderPath, seriesId);

            MasterMetadataHandler.Instance.Metadata.Series ??= [];
            MasterMetadataHandler.Instance.Metadata.Series.Add(cardSeries);
        }

        return cardSeries;
    }

    public static Dictionary<string, List<string>> DetermineFolderGrouping(string seriesSourceFolderPath)
    {
        var groupedFolders = new Dictionary<string, List<string>>();

        foreach (var setSourceFolderPath in Directory.EnumerateDirectories(seriesSourceFolderPath))
        {
            CardSetHelper.NormalizeAndGroupSetFoldersForDestination(setSourceFolderPath, groupedFolders);
        }

        if (Globals.ShowVerbose)
        {
            foreach (var value in groupedFolders)
            {
                Logger.LogAction($"Unique Folder Name: '{value.Key}'\n\t{string.Join("\n\t", value.Value)}\n", LogMessageType.Verbose);
            }
        }

        return groupedFolders;
    }
}