using System.Text.RegularExpressions;
using CardmastersOfTamriel.ImageProcessorConsole.Processors;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessorConsole.Factories;

public static class CardFactory
{
    public static List<Card> CreateCardsFromImagesAtFolderPath(CardSet set, HashSet<string> imageFilePaths, bool recordFilePathAsSource = true)
    {
        var cards = new List<Card>();
        foreach (var imageInfo in imageFilePaths.Select((path, index) => new { filePath = path, index }))
        {
            var imageIndex = imageInfo.index + 1;

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(imageInfo.filePath);
            string pattern = @".+_\d{3}$";
            var isMatch = Regex.IsMatch(fileNameWithoutExtension, pattern);

            var idToUse = isMatch ? fileNameWithoutExtension : Path.GetFileNameWithoutExtension(NameHelper.CreateImageFileName(set, (uint)imageIndex));

            var newCard = new Card
            {
                Id = idToUse,
                SetId = set.Id,
                SetDisplayName = set.DisplayName,
                SeriesId = set.SeriesId,
                ImageFileName = recordFilePathAsSource ? null : Path.GetFileName(imageInfo.filePath),
                Shape = null,
                DisplayName = null,
                DisplayedIndex = 0,
                DisplayedTotalCount = 0,
                Description = null,
                Tier = set.Tier,
                Value = set.DefaultValue,
                Weight = set.DefaultWeight,
                Keywords = set.DefaultKeywords,
                SourceAbsoluteFilePath = recordFilePathAsSource ? imageInfo.filePath : null,
                DestinationAbsoluteFilePath = recordFilePathAsSource ? null : imageInfo.filePath,
                DestinationRelativeFilePath = null,
            };

            cards.Add(newCard);
        }

        return cards;
    }

    public static List<Card> CreateCardsFromImagePaths(CardSet set, List<string> imageFilePathsAtDestination, uint actualTotalCardCountInSet)
    {
        var cards = new List<Card>();

        // Recreate card metadata from existing DDS files
        foreach (var (imagePath, index) in imageFilePathsAtDestination.Select((path, i) => (path, i + 1)))
        {
            var imageFileName = Path.GetFileName(imagePath);

            var newCard = new Card
            {
                Id = Path.GetFileNameWithoutExtension(imageFileName),
                SetId = set.Id,
                SetDisplayName = set.DisplayName,
                SeriesId = set.SeriesId,
                ImageFileName = imageFileName,
                Shape = null,
                DisplayName = Card.CreateGenericDisplayName(set.DisplayName, (uint)index, (uint)imageFilePathsAtDestination.Count),
                DisplayedIndex = (uint)index,
                DisplayedTotalCount = (uint)imageFilePathsAtDestination.Count,
                TrueIndex = 0,
                TrueTotalCount = actualTotalCardCountInSet,
                Description = string.Empty,
                Tier = set.Tier,
                Value = set.DefaultValue,
                Weight = set.DefaultWeight,
                Keywords = set.DefaultKeywords,
                SourceAbsoluteFilePath = null, // Since we don't have the original source file path
                DestinationAbsoluteFilePath = imagePath,
                DestinationRelativeFilePath = FilePathHelper.GetRelativePath(imagePath, set.Tier)
            };

            cards.Add(newCard);
        }

        return cards;
    }
}