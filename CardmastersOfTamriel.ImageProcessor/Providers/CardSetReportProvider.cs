using System.Text;
using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessor.Providers;

public class CardSetReportProvider
{
    private static readonly Lazy<Task<CardSetReportProvider>> _instance = new(() => CreateAsync(CancellationToken.None));
    private readonly string _filePath;

    private CardSetReportProvider(string filePath)
    {
        _filePath = filePath;
    }

    private static async Task<CardSetReportProvider> CreateAsync(CancellationToken cancellationToken = default)
    {
        var config = ConfigurationProvider.Instance.Config;

        var fileName = "CardMastersOfTamriel_Report_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
        var filePath = Path.Combine(config.Paths.OutputFolderPath, fileName);

        var instance = new CardSetReportProvider(filePath);

        if (!File.Exists(filePath))
        {
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            await writer.WriteLineAsync(
                "Tier,SeriesId,SetId,SavedCardsCount,SavedCardsWithDestinationsCount,ConvertedImages,TotalSetImagesAtSource,SetSourceFolderPath,SetDestinationFolderPath");
        }

        return instance;
    }

    public static Task<CardSetReportProvider> InstanceAsync(CancellationToken cancellationToken = default) => _instance.Value;

    public async Task UpdateWithSetInfoAsync(CardSet set, List<Card> savedCards, int convertedImageCount,
        int totalSetImagesAtSource)
    {
        var cardsWithDestinationFilePaths =
            savedCards.Where(card => !string.IsNullOrEmpty(card.DestinationAbsoluteFilePath)).ToList();
        await WriteToCsvAsync(
                  $"{set.Tier},{set.SeriesId},{set.Id},{savedCards.Count},{cardsWithDestinationFilePaths.Count},{convertedImageCount},{totalSetImagesAtSource},{set.SourceAbsoluteFolderPath},{set.DestinationAbsoluteFolderPath}");
    }

    private async Task WriteToCsvAsync(string message)
    {
        using var writer = new StreamWriter(_filePath, true, Encoding.UTF8);
        await writer.WriteLineAsync(message);
    }
}