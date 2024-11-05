using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessorConsole.Factories;

public static class CardSeriesFactory
{
    public static CardSeries CreateNewSeries(string seriesId, CardTier tier, CardSeries series)
    {
        return new CardSeries
        {
            Id = seriesId,
            Tier = tier,
            DisplayName = string.IsNullOrEmpty(series.DisplayName) ? NameHelper.FormatDisplayNameFromId(seriesId) : series.DisplayName,
            Theme = series.Theme ?? "",
            ReleaseDate = series.ReleaseDate ?? DateTime.UtcNow,
            Artist = series.Artist ?? "",
            IsLimitedEdition = series.IsLimitedEdition,
            Description = series.Description ?? "",
            Sets = [],
            SourceFolderPath = series.SourceFolderPath,
            DestinationFolderPath = series.DestinationFolderPath,
        };
    }
}