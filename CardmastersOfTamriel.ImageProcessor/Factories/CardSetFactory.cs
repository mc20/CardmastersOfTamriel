using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessor.Factories;

public static class CardSetFactory
{
    public static CardSet CreateNewSet(string setId, string folderName, CardSeries series)
    {
        return new CardSet(setId, series.Id)
        {
            SeriesKeyword = NamingHelper.CreateKeyword(series),
            DisplayName = NamingHelper.FormatDisplayNameFromFolderName(folderName),
            Tier = series.Tier,
            Description = "",
            Cards = [],
            SourceAbsoluteFolderPath = "",
            DestinationAbsoluteFolderPath = "",
            DestinationRelativeFolderPath = string.Empty
        };
    }
}