using System.Text;
using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessor.Providers;

public class CardSetReportProvider
{
    private static readonly Lazy<CardSetReportProvider> _instance = new();
    private readonly string _filePath;

    public CardSetReportProvider()
    {
        var config = ConfigurationProvider.Instance.Config;

        var fileName = "CardMastersOfTamriel_Report_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
        _filePath = Path.Combine(config.Paths.OutputFolderPath, fileName);

        if (File.Exists(_filePath)) return;
        using var writer = new StreamWriter(_filePath, false, Encoding.UTF8);
        writer.WriteLine(
            "Tier,SeriesId,SetId,SavedCardsCount,SavedCardsWithDestinationsCount,ConvertedImages,TotalSetImagesAtSource,SetSourceFolderPath,SetDestinationFolderPath");
    }

    public static CardSetReportProvider Instance => _instance.Value;

    public void UpdateWithSetInfo(CardSet set, List<Card> savedCards, int convertedImageCount,
        int totalSetImagesAtSource)
    {
        var cardsWithDestinationFilePaths =
            savedCards.Where(card => !string.IsNullOrEmpty(card.DestinationAbsoluteFilePath)).ToList();
        WriteToCsv(
            $"{set.Tier},{set.SeriesId},{set.Id},{savedCards.Count},{cardsWithDestinationFilePaths.Count},{convertedImageCount},{totalSetImagesAtSource},{set.SourceAbsoluteFolderPath},{set.DestinationAbsoluteFolderPath}");
    }

    private void WriteToCsv(string message)
    {
        using var writer = new StreamWriter(_filePath, true, Encoding.UTF8);
        writer.WriteLine(message);
    }
}