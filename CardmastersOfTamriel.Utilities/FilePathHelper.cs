using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.Utilities;

public static class FilePathHelper
{
    public static string GetRelativePath(string? absolutePath, CardTier tier)
    {
        var imagePath = absolutePath ?? string.Empty;
        var substring = tier.ToString();
        var index = imagePath.IndexOf(substring, StringComparison.OrdinalIgnoreCase);
        var path = index != -1 ? imagePath[(index + substring.Length)..] : string.Empty;
        return index != -1 ? Path.Join(substring, path) : string.Empty;
    }
}