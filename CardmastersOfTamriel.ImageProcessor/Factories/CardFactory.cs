using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessor.Factories;

public class CardFactory(Config config)
{
    public HashSet<Card> CreateCardsFromImagesAtFolderPath(CardSet set, HashSet<string> imageFilePaths)
    {
        var cards = new HashSet<Card>();
        foreach (var imageInfo in imageFilePaths.Select((path, index) => new { filePath = path, index }))
        {
            var imageIndex = imageInfo.index + 1;

            var newCardId = Path.GetFileNameWithoutExtension(NamingHelper.CreateImageFileName(set, (uint)imageIndex));
            var newCard = new Card(newCardId, set.Id)
            {
                SetDisplayName = set.DisplayName,
                SeriesId = set.SeriesId,
                Shape = null,
                ConversionDate = DateTime.Now,
                DisplayName = null,
                DisplayedIndex = 0,
                DisplayedTotalCount = 0,
                Description = null,
                Tier = set.Tier,
                Value = config.Defaults.DefaultCardValues.TryGetValue(set.Tier, out var value) ? value ?? 0 : 0,
                Weight = config.Defaults.DefaultCardWeights.GetValueOrDefault(set.Tier) ?? 0,
                Keywords = config.Defaults.DefaultMiscItemKeywords.ToHashSet(),
                SourceAbsoluteFilePath = imageInfo.filePath,
                DestinationAbsoluteFilePath = null,
                DestinationRelativeFilePath = null,
            };
            
            if (!string.IsNullOrEmpty(set.SeriesKeyword)) newCard.Keywords.Add(set.SeriesKeyword);

            cards.Add(newCard);
        }

        return cards;
    }
}