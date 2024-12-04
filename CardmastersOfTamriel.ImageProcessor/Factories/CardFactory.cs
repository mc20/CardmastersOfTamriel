using CardmastersOfTamriel.ImageProcessor.Configuration;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessor.Factories;

public class CardFactory(DefaultValuesForCards defaults)
{
    public HashSet<Card> CreateInitialCardsFromSource(CardSet set)
    {
        var cards = new HashSet<Card>();

        var imageFilePaths = ImageFilePathUtility.GetImageFilePathsFromFolder(set.SourceAbsoluteFolderPath).Order().ToHashSet();

        foreach (var imageInfo in imageFilePaths.Select((path, index) => new { filePath = path, index }))
        {
            var imageIndex = imageInfo.index + 1;

            var newCardId = Path.GetFileNameWithoutExtension(NamingHelper.CreateFileNameFromCardSetAndIndex(set, (uint)imageIndex, "dds"));
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
                Value = defaults.DefaultValues.TryGetValue(set.Tier, out var value) ? value ?? 0 : 0,
                Weight = defaults.DefaultWeights.GetValueOrDefault(set.Tier) ?? 0,
                Keywords = defaults.DefaultMiscItemKeywords.ToHashSet(),
                SourceAbsoluteFilePath = imageInfo.filePath,
                DestinationAbsoluteFilePath = null,
                DestinationRelativeFilePath = null
            };

            if (!string.IsNullOrEmpty(set.SeriesKeyword)) newCard.Keywords.Add(set.SeriesKeyword);

            cards.Add(newCard);
        }

        return cards;
    }
}