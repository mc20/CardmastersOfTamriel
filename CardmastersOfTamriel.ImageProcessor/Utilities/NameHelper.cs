using System.Globalization;
using System.Text.RegularExpressions;
using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public static partial class NameHelper
{
    private static readonly Regex NameNormalizerRegex = MyRegex();

    public static string FormatDisplayNameFromId(string setId)
    {
        var textInfo = CultureInfo.CurrentCulture.TextInfo;

        // Split and capitalize words
        var words = setId.Split('_').Select(word => textInfo.ToTitleCase(word.ToLower())).ToList();

        var mainText = string.Join(" ", words);
        var lastWord = words.Last();

        // Check if the last word is a number
        if (!int.TryParse(lastWord, out var numericValue)) return mainText;

        words.RemoveAt(words.Count - 1); // Remove the numeric part from the main text
        mainText = string.Join(" ", words);

        return $"{mainText} (Set {numericValue})";
    }

    public static string NormalizeName(string name)
    {
        // Convert to lowercase
        name = name.ToLower();

        // Replace spaces with underscores
        name = name.Replace(" ", "_");

        // Remove any non-alphanumeric characters except underscores
        name = NameNormalizerRegex.Replace(name, "");

        return name;
    }

    public static string CreateImageFileName(CardSet set, uint imageIndex)
    {
        return $"{set.Id}_{imageIndex:D3}.dds";
    }

    [GeneratedRegex(@"[^a-z0-9_]", RegexOptions.Compiled)]
    private static partial Regex MyRegex();

    public static readonly int MaxCardShapeTextLength = Enum.GetValues<CardShape>().Max(shape => shape.ToString().Length);
}