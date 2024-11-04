using System.Text.Json;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public class CardSetProcessor
{
    private readonly AppConfig _appConfig;
    private readonly MasterMetadataHandler _handler;
    private const int MaxSampleSize = 20;

    public CardSetProcessor(AppConfig appConfig, MasterMetadataHandler handler)
    {
        _appConfig = appConfig;
        _handler = handler;
    }

    public void ProcessSetAndImages(CardSet set)
    {
        Log.Information($"Processing Set from Source Path: '{set.SourceFolderPath}'");
        Log.Verbose($"Destination Set Folder Path: '{set.DestinationFolderPath}'");

        if (set.Id == null) return;

        var savedJsonFilePath = Path.Combine(set.DestinationFolderPath, "cards.jsonl");
        var savedJsonBackupFilePath = Path.Combine(set.DestinationFolderPath, "cards.jsonl.backup");

        var cardsFromMetadataFile = new List<Card>();

        if (File.Exists(savedJsonFilePath))
        {
            Log.Verbose($"Existing Card log found at '{savedJsonFilePath}'");

            // If a backup already exists, delete it
            if (File.Exists(savedJsonBackupFilePath))
            {
                File.Delete(savedJsonBackupFilePath);
            }

            // Move the original file to create a backup
            File.Copy(savedJsonFilePath, savedJsonBackupFilePath);

            var lines = File.ReadLines(savedJsonFilePath);
            cardsFromMetadataFile = lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => JsonSerializer.Deserialize<Card>(line, JsonSettings.Options))
                .Where(card => card != null)
                .Select(card => card!) // Use the null-forgiving operator to ensure the list is non-nullable
                .ToList();

            File.Delete(savedJsonFilePath);
        }

        Log.Verbose($"New Card log will be saved to '{savedJsonFilePath}'");
        File.Create(savedJsonFilePath).Dispose(); // Dispose to close the file handle

        set.Cards ??= [];
        set.Cards.Clear();

        var imageFilesAtSource = GetImageFilePathsFromFolder(set.SourceFolderPath).OrderBy(file => file).ToList();

        if (imageFilesAtSource.Count == 0)
        {
            Log.Warning($"No images found in '{set.SourceFolderPath}'");
            _handler.WriteMetadataToFile();
            return;
        }

        var actualImagesAtDestinationFolder = GetImageFilePathsFromFolder(set.DestinationFolderPath, ["*.dds"]);

        Log.Verbose($"Found {imageFilesAtSource.Count} images at Source Set Folder Path: '{set.SourceFolderPath}'");
        Log.Verbose(
            $"Found {cardsFromMetadataFile.Count} Cards previously saved at Destination Set Folder Path: '{set.DestinationFolderPath}'");
        Log.Verbose(
            $"Found {actualImagesAtDestinationFolder.Count} converted images previously saved at Destination Set Folder Path: '{set.DestinationFolderPath}'");


        var cardsFromMetadataFilePreviouslyConverted = cardsFromMetadataFile
            .Where(card => !string.IsNullOrWhiteSpace(card.DestinationFilePath))
            .Select(card => card.DestinationFilePath!).ToList();

        // Find images that are in the destination folder but not in the metadata file
        var unmatchedImages = actualImagesAtDestinationFolder
            .Except(cardsFromMetadataFilePreviouslyConverted)
            .ToList();

        // Find images that are in the metadata file but not in the destination folder
        var missingImages = cardsFromMetadataFilePreviouslyConverted
            .Except(actualImagesAtDestinationFolder)
            .ToList();

        // Record warnings if there are mismatches
        if (unmatchedImages.Any())
        {
            Log.Warning("The following images are in the destination folder but not in the metadata file:");
            foreach (var image in unmatchedImages)
            {
                Log.Warning(image);
            }
        }

        if (missingImages.Any())
        {
            Log.Warning("The following images are in the metadata file but not in the destination folder:");
            foreach (var image in missingImages)
            {
                Log.Warning(image);
            }
        }

        var convertedCardsAtDestination =
            cardsFromMetadataFile.Where(card => !string.IsNullOrEmpty(card.DestinationFilePath)).ToList();

        Log.Verbose(
            $"Found {convertedCardsAtDestination.Count} Cards previously converted at Destination Set Folder Path: '{set.DestinationFolderPath}'");

        var eligibleImageFilePaths = new List<string>();

        foreach (var imagePath in imageFilesAtSource)
        {
            if (!convertedCardsAtDestination.Select(card => card.SourceFilePath).Contains(imagePath))
            {
                eligibleImageFilePaths.Add(imagePath);
            }
        }

        Log.Verbose(
            $"{eligibleImageFilePaths.Count} images were found to be eligible from Source Path: '{set.SourceFolderPath}'");

        var numberOfRemainingCardsRequired = MaxSampleSize - convertedCardsAtDestination.Count;

        Log.Verbose(
            $"{numberOfRemainingCardsRequired} more images from Source Set Path '{set.SourceFolderPath}' are required for this Destination Set Folder Path: '{set.DestinationFolderPath}'");

        if (numberOfRemainingCardsRequired <= 0)
        {
            if (set.AutoGenerateCardNames)
            {
                foreach (var savedCard in convertedCardsAtDestination.Select(
                             (card, index) => new { index, card }))
                {
                    savedCard.card.Id = Path.GetFileNameWithoutExtension(savedCard.card.DestinationFilePath);
                    savedCard.card.DisplayName =
                        $"{set.DisplayName} - Card #{(savedCard.index + 1)} of {savedCard.card.TotalCount}";
                }
            }

            set.Cards = cardsFromMetadataFile;
            foreach (var card in cardsFromMetadataFile.OrderBy(card => card.Id))
                FileOperations.AppendCardToFile(card, savedJsonFilePath);

            _handler.WriteMetadataToFile();
            return;
        }

        var selectedImageFilePaths = SelectRandomImageFilePaths(numberOfRemainingCardsRequired, eligibleImageFilePaths);

        Log.Verbose(
            $"{selectedImageFilePaths.Count} images from '{set.SourceFolderPath}' were selected to be converted and saved at Destination Set Folder Path: '{set.DestinationFolderPath}'");

        ProcessImagesAndGenerateCards(set, imageFilesAtSource, selectedImageFilePaths, MaxSampleSize,
            savedJsonFilePath);

        // var data = _handler.Metadata.Series?.SelectMany(series => series?.Sets ?? [])
        //     .FirstOrDefault(metaSet => metaSet.Id == set.Id);
        //
        // _handler.Metadata.Series?.SelectMany(series => series?.Sets ?? [])

        _handler.WriteMetadataToFile();
    }

    private static List<string> GetImageFilePathsFromFolder(string folderPath, string[]? imageExtensions = null)
    {
        imageExtensions ??= ["*.png", "*.jpg", "*.jpeg"];
        return imageExtensions.SelectMany(ext => Directory.EnumerateFiles(folderPath, ext)).ToList();
    }

    private static List<string> SelectRandomImageFilePaths(int maxSampleSize, List<string> imageFilePaths)
    {
        Log.Information($"Selecting {maxSampleSize} random images provided image path list");
        // Log.Verbose($"Image File Paths to choose from are {string.Join(", ", imageFilePaths)}");

        // Randomly select a sample of images (adjust the sample size as needed)
        var random = new Random();
        return imageFilePaths.OrderBy(_ => random.Next()).Take(maxSampleSize).ToList();
    }

    private void ProcessImagesAndGenerateCards(CardSet set, List<string> allCards, List<string> imageFilesToConvert,
        int totalCount, string jsonFilePath)
    {
        var selectedCardIndex = 0;
        foreach (var imageInfo in allCards.Select((path, index) => new { filePath = path, index }))
        {
            var imageIndex = imageInfo.index + 1;
            var imageFileName = $"{set.Id}_{imageIndex:D3}.dds";

            var imageDisplayName = "";
            var imageDestinationFilePath = "";
            if (imageFilesToConvert.Contains(imageInfo.filePath))
            {
                selectedCardIndex++;
                (imageDisplayName, imageDestinationFilePath) =
                    ProcessImageFile(set, imageInfo.filePath, totalCount, selectedCardIndex, imageFileName);
                Log.Verbose(
                    $"Converted Card from Source File Path '{imageInfo.filePath}' to Destination File Path '{imageFileName}'");
            }

            var newCard = CardFactory.CreateCard(set, imageInfo.filePath, imageFileName, imageIndex,
                imageFilesToConvert.Count,
                imageDisplayName, imageDestinationFilePath);
            set.Cards ??= [];
            set.Cards.Add(newCard);

            Log.Information(
                $"Saved Card {newCard.Id} having Destination File Path '{newCard.DestinationFilePath}' to metadata file '{jsonFilePath}'");

            FileOperations.AppendCardToFile(newCard, jsonFilePath);
        }
    }

    private (string imageDisplayName, string imageDestinationFilePath) ProcessImageFile(CardSet set,
        string sourceImageFilePath, int totalCount, int selectedCardIndex, string imageFileName)
    {
        var imageDisplayName = "";
        var imageDestinationFilePath = "";

        imageDisplayName = $"{set.DisplayName} - Card #{selectedCardIndex} of {totalCount}";
        imageDestinationFilePath = Path.Combine(set.DestinationFolderPath, imageFileName);

        var helper = new ImageHelper(_appConfig);
        helper.ConvertImageAndSaveToDestination(sourceImageFilePath, imageDestinationFilePath);

        return (imageDisplayName, imageDestinationFilePath);
    }
}