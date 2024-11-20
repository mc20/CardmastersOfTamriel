using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessor.Factories;

public static class CardSetFactory
{
    public static CardSet CreateNewSet(string setId, CardSeries series)
    {
        var config = ConfigurationProvider.Instance.Config;

        return new CardSet(setId, series.Id)
        {
            DisplayName = NameHelper.FormatDisplayNameFromId(setId),
            Tier = series.Tier,
            Description = "",
            Cards = [],
            SourceAbsoluteFolderPath = "",
            DestinationAbsoluteFolderPath = "",
            DefaultValue = 0,
            DefaultWeight = 0,
            DefaultKeywords = config.General.DefaultMiscItemKeywords,
            DestinationRelativeFolderPath = string.Empty,
        };
    }
}