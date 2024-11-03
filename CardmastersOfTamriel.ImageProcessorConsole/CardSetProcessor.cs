using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public class CardSetProcessor
{
    private readonly ImageHelper _imageHelper;
    private const int MaxSampleSize = 20;

    public CardSetProcessor(ImageHelper imageHelper)
    {
        _imageHelper = imageHelper;
    }

    public void ProcessSetAndImages(CardSet set)
    {
        Logger.LogAction($"Processing Set: '{set.Id}' at '{set.SourceFolderPath}'");
        Logger.LogAction($"Destination Folder Path: '{set.DestinationFolderPath}'", LogMessageType.Verbose);

        if (set.Id == null) return;

        var imageFiles = GetImageFiles(set.SourceFolderPath);
        var selectedImages = SelectRandomImages(imageFiles);
        var filePath = Path.Combine(set.DestinationFolderPath, "cards.jsonl");

        Logger.LogAction($"Card log will be saved to '{filePath}'", LogMessageType.Verbose);

        File.Delete(filePath);

        ProcessImagesAndGenerateCards(set, imageFiles, selectedImages, filePath);

        MasterMetadataHandler.Instance.WriteMetadataToFile();

    }

    private static IEnumerable<string> GetImageFiles(string sourceFolderPath)
    {
        var imageExtensions = new[] { "*.png", "*.jpg", "*.jpeg" };
        return imageExtensions.SelectMany(ext => Directory.EnumerateFiles(sourceFolderPath, ext));
    }

    private static List<string> SelectRandomImages(IEnumerable<string> imageFilePaths)
    {
        // Randomly select a sample of images (adjust the sample size as needed)
        var totalImageCount = imageFilePaths.Count();
        var sampleSize = Math.Min(MaxSampleSize, totalImageCount);
        var random = new Random();
        return imageFilePaths.OrderBy(_ => random.Next()).Take(sampleSize).ToList();
    }

    private void ProcessImagesAndGenerateCards(CardSet set, IEnumerable<string> imageFiles, List<string> selectedImages, string filePath)
    {
        var selectedCardIndex = 0;
        foreach (var imageInfo in imageFiles.Select((filePath, index) => new { filePath, index }))
        {
            var imageIndex = imageInfo.index + 1;
            var imageFileName = $"{set.Id}_{imageIndex:D3}.dds";

            var (imageDisplayName, imageDestinationFilePath) = ProcessImageFile(set, imageInfo.filePath, selectedImages, ref selectedCardIndex, imageFileName);

            var newCard = CardFactory.CreateCard(set, imageInfo.filePath, imageFileName, imageIndex, imageFiles.Count(), imageDisplayName, imageDestinationFilePath);
            set.Cards ??= [];
            set.Cards.Add(newCard);

            FileOperations.AppendCardToFile(newCard, filePath);

            if (!string.IsNullOrEmpty(imageDestinationFilePath))
            {
                Logger.LogAction($"Created Card: '{newCard.ImageFileName}' at '{imageDestinationFilePath}'", LogMessageType.Verbose);
            }
        }
    }

    private (string imageDisplayName, string imageDestinationFilePath) ProcessImageFile(CardSet set, string imageFilePath, List<string> selectedImages, ref int selectedCardIndex, string imageFileName)
    {
        string imageDisplayName = "";
        string imageDestinationFilePath = "";

        if (selectedImages.Contains(imageFilePath))
        {
            selectedCardIndex++;
            imageDisplayName = $"{set.DisplayName} - Card #{selectedCardIndex} of {selectedImages.Count}";
            imageDestinationFilePath = Path.Combine(set.DestinationFolderPath, imageFileName);
            Logger.LogAction($"Converting Image: '{imageFilePath}' to '{imageDestinationFilePath}'", LogMessageType.Verbose);

            _imageHelper.ProcessImage(imageFilePath, imageDestinationFilePath);
        }

        return (imageDisplayName, imageDestinationFilePath);
    }


}