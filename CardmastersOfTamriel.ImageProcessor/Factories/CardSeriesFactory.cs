using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessor.Factories;

public static class CardSeriesFactory
{
    public static CardSeries CreateNewSeries(string seriesId, CardTier tier)
    {
        return new CardSeries(seriesId)
        {
            DisplayName = NameHelper.FormatDisplayNameFromId(seriesId),
            Tier = tier,
            Description = string.Empty,
            Sets = [],
            SourceFolderPath = "",
            DestinationFolderPath = "",
        };
    }
}