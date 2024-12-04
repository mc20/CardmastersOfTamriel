using System.Globalization;
using System.Text.RegularExpressions;
using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.Utilities;

public static partial class NamingHelper
{
    private static readonly Regex NameNormalizerRegex = MyRegex();

    /// <summary>
    ///     Formats a display name from a given folder name by capitalizing words and removing GUIDs.
    /// </summary>
    /// <param name="folderName">The folder name to format.</param>
    /// <returns>
    ///     A formatted display name. If the last word in the folder name is a number, it is treated as a set number
    ///     and appended to the display name in the format "(Set {number})".
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the folder path does not contain a valid name.</exception>
    public static string FormatDisplayNameFromFolderName(string folderName)
    {
        var textInfo = CultureInfo.CurrentCulture.TextInfo;

        var directoryName = Path.GetFileName(folderName);
        if (string.IsNullOrWhiteSpace(directoryName))
            throw new InvalidOperationException($"Unable to create a name from the provided folder path: '{folderName}'");

        // Split and capitalize words
        var words = directoryName
            .Split('_')
            .Select(word => textInfo.ToTitleCase(word.ToLower()))
            .ToList();

        // Remove GUIDs (assuming they are in the format of 8-4-4-4-12 hexadecimal characters)
        words = words.Where(word => !GuidRegex().IsMatch(word)).ToList();

        var mainText = string.Join(" ", words);
        var lastWord = words.LastOrDefault();

        // Check if the last word is a number
        if (lastWord == null || !int.TryParse(lastWord, out var numericValue)) return mainText;

        words.RemoveAt(words.Count - 1); // Remove the numeric part from the main text
        mainText = string.Join(" ", words);
        return $"{mainText} (Set {numericValue})";
    }

    public static string NormalizeName(string name)
    {
        name = name.ToLower();

        name = name.Replace(" ", "_");
        name = name.Replace("-", "_");
        name = name.Replace("&", "and");

        // Remove any non-alphanumeric characters except underscores
        name = NameNormalizerRegex.Replace(name, "");

        return name;
    }

    public static string CreateKeyword(CardSeries cardSeries)
    {
        return (cardSeries.DisplayName is null ? cardSeries.Id : NormalizeName(cardSeries.DisplayName)).AddModNamePrefix();
    }

    public static string CreateFileNameFromCardSetAndIndex(CardSet set, uint imageIndex, string extension)
    {
        return $"{CreateCardId(set.Id, imageIndex)}.{extension}";
    }
    
    public static string CreateFileNameFromCardSetIdAndIndex(string setId, uint imageIndex, string extension)
    {
        return $"{CreateCardId(setId, imageIndex)}.{extension}";
    }
    
    public static string CreateCardId(string cardSetId, uint imageIndex)
    {
        return $"{cardSetId}_{imageIndex:D3}";
    }

    [GeneratedRegex(@"[^a-zA-Z0-9_]", RegexOptions.Compiled)]
    private static partial Regex MyRegex();

    [GeneratedRegex(@"^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GuidRegex();
}