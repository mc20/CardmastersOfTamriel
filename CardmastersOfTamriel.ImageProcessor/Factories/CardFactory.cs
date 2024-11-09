using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessor.Factories;

public static class CardFactory
{
    public static HashSet<Card> CreateCardsFromImagesAtFolderPath(CardSet set, HashSet<string> imageFilePaths,
        bool recordFilePathAsSource = true)
    {
        var cards = new HashSet<Card>();
        foreach (var imageInfo in imageFilePaths.Select((path, index) => new { filePath = path, index }))
        {
            var imageIndex = imageInfo.index + 1;

            var newCardId = Path.GetFileNameWithoutExtension(NameHelper.CreateImageFileName(set, (uint)imageIndex));
            var newCard = new Card(newCardId, set.Id)
            {
                SetDisplayName = set.DisplayName,
                SeriesId = set.SeriesId,
                Shape = null,
                ConversionDate = DateTime.UtcNow,
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
}