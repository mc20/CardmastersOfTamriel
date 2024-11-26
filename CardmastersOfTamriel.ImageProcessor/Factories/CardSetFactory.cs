using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessor.Factories;

public static class CardSetFactory
{
    public static CardSet CreateNewSet(string setId, string folderName, CardSeries series, HashSet<string> defaultKeywords)
    {
        return new CardSet(setId, series.Id)
        {
            DisplayName = NamingHelper.FormatDisplayNameFromFolderName(folderName),
            Tier = series.Tier,
            Description = "",
            Cards = [],
            SourceAbsoluteFolderPath = "",
            DestinationAbsoluteFolderPath = "",
            DefaultValue = 0,
            DefaultWeight = 0,
            DefaultKeywords = defaultKeywords,
            DestinationRelativeFolderPath = string.Empty,
        };
    }
}