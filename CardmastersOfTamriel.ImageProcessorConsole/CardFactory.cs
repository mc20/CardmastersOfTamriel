using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public static class CardFactory
{
    public static Card CreateCard(CardSet set, string sourceFilePath, string imageFileName, int imageIndex, int totalImageCount, string displayName, string destinationFilePath)
    {
        return new Card
        {
            Id = imageFileName,
            SetId = set.Id,
            SetDisplayName = set.DisplayName,
            SeriesId = set.SeriesId,
            ImageFileName = imageFileName,
            Shape = CardShape.Portrait,
            DisplayName = displayName,
            Index = imageIndex,
            TotalCount = totalImageCount,
            Description = "",
            Tier = CardTier.Tier1,
            Value = 0,
            Weight = 0,
            Keywords = [],
            SourceFilePath = sourceFilePath,
            DestinationFilePath = destinationFilePath
        };
    }

}