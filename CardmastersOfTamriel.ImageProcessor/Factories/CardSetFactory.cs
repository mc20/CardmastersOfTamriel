using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessorConsole.Factories;

public static class CardSetFactory
{
    public static CardSet CreateNewSet(string setId, CardSeries series)
    {
        return new CardSet
        {
            Id = setId,
            SeriesId = series.Id,
            DisplayName = NameHelper.FormatDisplayNameFromId(setId),
            Tier = series.Tier,
            Theme = "",
            Description = "",
            ReleaseDate = DateTime.UtcNow,
            Artist = "",
            IsLimitedEdition = false,
            Cards = [],
            AutoRegenerateData = true,
            CollectorsNote = "",
            Region = "",
            ExtraAttributes = new Dictionary<string, object>(),
            SourceAbsoluteFolderPath = "",
            DestinationAbsoluteFolderPath = "",
            DefaultValue = 0,
            DefaultWeight = 0,
            DefaultKeywords = ["VendorItemMisc"],
        };
    }
}
