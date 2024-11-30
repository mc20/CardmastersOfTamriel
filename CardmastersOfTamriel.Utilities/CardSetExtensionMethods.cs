using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.Utilities;

public static class CardSetExtensionMethods
{
    public static void OverwriteWith(this CardSet set, CardSet otherCardSet)
    {
        set.DisplayName = otherCardSet.DisplayName;
        set.Tier = otherCardSet.Tier;
        set.Description = otherCardSet.Description;
        set.SourceAbsoluteFolderPath = otherCardSet.SourceAbsoluteFolderPath;
        set.DestinationAbsoluteFolderPath = otherCardSet.DestinationAbsoluteFolderPath;
        set.DestinationRelativeFolderPath = otherCardSet.DestinationRelativeFolderPath;
    }

    public static bool OverwriteWith(this CardSet set, CardSetHandlerOverrideData data)
    {
        var isOverwritten = false;
        if (string.IsNullOrWhiteSpace(data.NewSetDisplayName) || set.DisplayName == data.NewSetDisplayName) return isOverwritten;
        
        set.DisplayName = data.NewSetDisplayName;
        isOverwritten = true;

        return isOverwritten;

    }
}