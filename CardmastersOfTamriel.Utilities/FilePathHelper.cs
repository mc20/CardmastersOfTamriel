using CardmastersOfTamriel.Models;
using Serilog;

namespace CardmastersOfTamriel.Utilities;

public static class Extensions
{
    public static void SetRelativePath(this Card card)
    {
        card.DestinationRelativeFilePath = FilePathHelper.GetRelativePath(card.DestinationAbsoluteFilePath ?? string.Empty, card.Tier);
    }
}

public static class FilePathHelper
{
    public static string GetRelativePath(string absolutePath, CardTier tier)
    {
        var imagePath = absolutePath ?? string.Empty;
        var substring = tier.ToString();
        int index = imagePath.IndexOf(substring, StringComparison.OrdinalIgnoreCase);
        var path = (index != -1) ? imagePath.Substring(index + substring.Length) : string.Empty;
        return (index != -1) ? Path.Join(substring, path) : string.Empty;
    }
}