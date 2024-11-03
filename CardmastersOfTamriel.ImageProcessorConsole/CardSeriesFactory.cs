using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public static class CardSeriesFactory
{
    public static CardSeries CreateNewSeries(string seriesId, CardTier tier)
    {
        return new CardSeries
        {
            Id = seriesId,
            Tier = tier,
            DisplayName = NameHelper.FormatDisplayNameFromId(seriesId),
            Theme = "",
            ReleaseDate = DateTime.UtcNow,
            Artist = "",
            IsLimitedEdition = false,
            Description = "",
            Sets = [],
            SourceFolderPath = "",
            DestinationFolderPath = ""
        };
    }
}