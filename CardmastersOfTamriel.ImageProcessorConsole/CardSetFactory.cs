using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public static class CardSetFactory
{
    public static CardSet CreateNewSet(string setId, CardSeries series)
    {
        return new CardSet
        {
            Id = setId,
            SeriesId = series.Id,
            DisplayName = series.DisplayName,
            Theme = "",
            ReleaseDate = DateTime.UtcNow,
            Artist = "",
            IsLimitedEdition = false,
            Description = "",
            Cards = [],
            CollectorsNote = "",
            Region = "",
            ExtraAttributes = new Dictionary<string, object>(),
            SourceFolderPath = "",
            DestinationFolderPath = ""
        };
    }
}
