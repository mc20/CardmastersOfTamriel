using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessor.Factories;

public static class CardSeriesFactory
{
    public static CardSeries CreateNewSeries(string seriesId, CardTier tier, HashSet<CardSet>? sets = null)
    {
        return new CardSeries(seriesId)
        {
            DisplayName = NameHelper.FormatDisplayNameFromId(seriesId),
            Tier = tier,
            Description = string.Empty,
            Sets = sets ?? [],
            SourceFolderPath = "",
            DestinationFolderPath = "",
        };
    }
}