using System.Text.Json;
using CardmastersOfTamriel.ImageProcessor.Factories;
using CardmastersOfTamriel.ImageProcessor.Models;
using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class CardSetImageConversionHandler : ICardSetHandler
{
    private readonly Config _config = ConfigurationProvider.Instance.Config;

    public void ProcessCardSet(CardSet set)
    {
        Log.Information($"Processing Set from Source Path: '{set.SourceAbsoluteFolderPath}'");

        set.Cards ??= [];
        set.Cards.Clear();

        var savedJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "cards.jsonl");
        var savedJsonBackupFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "cards.jsonl.backup");

        var cardsFromMetadataFile = LoadCardsFromJsonFile(savedJsonFilePath, savedJsonBackupFilePath);

        Log.Verbose($"Loaded {cardsFromMetadataFile.Count} cards from metadata file");

        var imageFilePathsAtSource = CardSetImageHelper
            .GetImageFilePathsFromFolder(set.SourceAbsoluteFolderPath ?? string.Empty)
            .OrderBy(file => file).ToList();

        Log.Verbose($"Found {imageFilePathsAtSource.Count} images at source path");

        var cardsFromSource = CardFactory.CreateCardsFromImagesAtFolderPath(set, [.. imageFilePathsAtSource], true);

        Log.Verbose($"Created {cardsFromSource.Count} cards from source images");

        var updatedCards = cardsFromMetadataFile.ConsolidateCardsWith(cardsFromSource);

        Log.Verbose($"Consolidated {updatedCards.Count} cards from metadata and source images");

        var imageFilePathsAtDestination =
            CardSetImageHelper.GetImageFilePathsFromFolder(set.DestinationAbsoluteFolderPath, ["*.dds"]);

        Log.Verbose($"Found {imageFilePathsAtDestination.Count} DDS images at destination path");

        var cardsAtDestination =
            CardFactory.CreateCardsFromImagesAtFolderPath(set, [.. imageFilePathsAtDestination], false);

        Log.Verbose($"Created {cardsAtDestination.Count} cards from destination images");

        var finalCards = updatedCards.ConsolidateCardsWith(cardsAtDestination).ToList();

        Log.Verbose($"Consolidated {finalCards.Count} cards from metadata, source, and destination images");

        ProcessEligibleImages(set, finalCards, cardsAtDestination);

        UpdateDisplayCards(set, finalCards);

        finalCards.ForEach(card => FileOperations.AppendDataToFile(card, savedJsonFilePath));

        set.Cards = finalCards.ToHashSet();

        var handler = MasterMetadataProvider.Instance.MetadataHandler;
        handler.WriteMetadataToFile();
    }

    private static List<Card> LoadCardsFromJsonFile(string savedJsonFilePath, string savedJsonBackupFilePath)
    {
        if (!File.Exists(savedJsonFilePath)) return [];

        if (File.Exists(savedJsonBackupFilePath))
        {
            File.Delete(savedJsonBackupFilePath);
        }

        File.Copy(savedJsonFilePath, savedJsonBackupFilePath);

        var lines = new HashSet<string>(File.ReadLines(savedJsonFilePath));
        var uniqueCards = new HashSet<string>();

        var cardsFromMetadataFile = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<Card>(line, JsonSettings.Options))
            .Where(card => card != null && uniqueCards.Add(card.Id!))
            .Select(card => card!)
            .ToList();

        File.Delete(savedJsonFilePath);

        return cardsFromMetadataFile;
    }

    private void ProcessEligibleImages(CardSet set, List<Card> finalCards, HashSet<Card> cardsAtDestination)
    {
        var eligibleFilePathsForConversion = finalCards.Select(card => card?.SourceAbsoluteFilePath ?? string.Empty)
            .Where(filePath => !string.IsNullOrWhiteSpace(filePath)).ToHashSet();

        Log.Information(
            $"Found {eligibleFilePathsForConversion.Count} eligible images for conversion (no destination specified)");

        Log.Information(
            $"MaxSampleSize is {_config.General.MaxSampleSize} and Available Card Count is {finalCards.Count}");

        var maximumNumberOfCards = Math.Min(_config.General.MaxSampleSize, finalCards.Count);

        var needMoreRandomCards = cardsAtDestination.Count < maximumNumberOfCards;

        Log.Information(
            $"Maximum Number of Cards: {maximumNumberOfCards} while there are {cardsAtDestination.Count} cards at destination. Need more random cards? {needMoreRandomCards}");

        var randomCards = needMoreRandomCards
            ? CardSetImageHelper.SelectRandomImageFilePaths(maximumNumberOfCards - cardsAtDestination.Count,
                eligibleFilePathsForConversion)
            : [];

        if (needMoreRandomCards)
        {
            Log.Information($"Selected {randomCards.Count} random images for conversion");
        }
        else
        {
            Log.Information("No more random images needed for conversion");
        }


        foreach (var info in finalCards.OrderBy(card => card.Id).Select((card, index) => (card, index)))
        {
            if (!string.IsNullOrWhiteSpace(info.card.SourceAbsoluteFilePath) &&
                randomCards.Contains(info.card.SourceAbsoluteFilePath))
            {
                Log.Information($"Processing Card {Path.GetFileName(info.card.SourceAbsoluteFilePath)} for conversion");

                var result = ConvertAndSaveImage(set, info.card.SourceAbsoluteFilePath,
                    NameHelper.CreateImageFileName(set, (uint)info.index + 1));

                info.card.ConversionDate = DateTime.UtcNow;
                info.card.Shape = result.Shape;
                info.card.DisplayName = null;
                info.card.DestinationAbsoluteFilePath = result.DestinationAbsoluteFilePath;
                info.card.DestinationRelativeFilePath =
                    FilePathHelper.GetRelativePath(result.DestinationAbsoluteFilePath, set.Tier);
                info.card.DisplayedIndex = 0;
                info.card.DisplayedTotalCount = 0;
                info.card.TrueIndex = (uint)info.index + 1;
                info.card.TrueTotalCount = (uint)finalCards.Count;
                info.card.SetGenericDisplayName();
            }
            else
            {
                if (string.IsNullOrEmpty(info.card.DestinationAbsoluteFilePath))
                {
                    Log.Verbose($"Card {info.card.Id} was not converted and will be skipped");
                    info.card.Shape =
                        ImageHelper.DetermineOptimalShape(info.card
                            .SourceAbsoluteFilePath!); // Keep track of the shape for future reference
                    info.card.DisplayName = null;
                    info.card.DestinationAbsoluteFilePath = null;
                    info.card.DestinationRelativeFilePath = null;
                    info.card.DisplayedIndex = 0;
                    info.card.DisplayedTotalCount = 0;
                    info.card.TrueIndex = (uint)info.index + 1;
                    info.card.TrueTotalCount = (uint)finalCards.Count;
                }
                else
                {
                    info.card.DestinationRelativeFilePath =
                        FilePathHelper.GetRelativePath(info.card.DestinationAbsoluteFilePath, set.Tier);
                    Log.Verbose($"Card {info.card.Id} was possibly already converted and will be used as-is");
                }
            }
        }
    }

    private static void UpdateDisplayCards(CardSet set, List<Card> finalCards)
    {
        var cardsEligibleForDisplay =
            finalCards.Where(card => !string.IsNullOrWhiteSpace(card.DestinationAbsoluteFilePath)).ToList();
        foreach (var cardInfo in cardsEligibleForDisplay.OrderBy(card => card.Id)
                     .Select((card, index) => (card, index)))
        {
            cardInfo.card.DisplayedIndex = (uint)cardInfo.index + 1;
            cardInfo.card.DisplayedTotalCount = (uint)cardsEligibleForDisplay.Count;
            cardInfo.card.SetGenericDisplayName();
        }
    }

    private static ConversionResult ConvertAndSaveImage(CardSet set, string sourceImageFilePath, string imageFileName)
    {
        var imageDestinationFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, imageFileName);

        var helper = new ImageConverter();
        var imageShape =
            helper.ConvertImageAndSaveToDestination(set.Tier, sourceImageFilePath, imageDestinationFilePath);

        return new ConversionResult()
        {
            Shape = imageShape,
            DestinationAbsoluteFilePath = imageDestinationFilePath
        };
    }
}