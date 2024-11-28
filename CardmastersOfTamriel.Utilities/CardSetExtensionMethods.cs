using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.Utilities;

public static class CardSetExtensionMethods
{
    public static void OverrideWith(this CardSet set, CardSet otherCardSet)
    {
        set.DisplayName = otherCardSet.DisplayName;
        set.Tier = otherCardSet.Tier;
        set.Description = otherCardSet.Description;
        set.SourceAbsoluteFolderPath = otherCardSet.SourceAbsoluteFolderPath;
        set.DestinationAbsoluteFolderPath = otherCardSet.DestinationAbsoluteFolderPath;
        set.DestinationRelativeFolderPath = otherCardSet.DestinationRelativeFolderPath;
    }
}