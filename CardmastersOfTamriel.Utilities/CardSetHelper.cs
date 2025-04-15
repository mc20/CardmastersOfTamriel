using System.Text.RegularExpressions;
using Serilog;

namespace CardmastersOfTamriel.Utilities;

public static partial class CardSetHelper
{
    public static HashSet<string> GroupAndNormalizeFolderNames(string setSourceFolderPath,
        Dictionary<string, List<string>> groupedFolders)
    {
        var anomalies = new HashSet<string>();

        var originalFolderName = Path.GetFileName(setSourceFolderPath);
        var folderPattern = MyRegex();
        var match = folderPattern.Match(originalFolderName);

        if (!match.Success) anomalies.Add(setSourceFolderPath);

        var uniqueFolderName = NamingHelper.NormalizeName(match.Groups[1].Value);

        Log.Debug($"Normalized name of '{setSourceFolderPath}' determined to be '{uniqueFolderName}'");

        if (groupedFolders.TryGetValue(uniqueFolderName, out var sourceSetFolderPaths))
            sourceSetFolderPaths.Add(setSourceFolderPath);
        else
            groupedFolders[uniqueFolderName] = [setSourceFolderPath];

        return anomalies;
    }

    [GeneratedRegex(@"^([a-zA-Z]+(?:[ _][a-zA-Z]+)*)", RegexOptions.IgnoreCase, "en-CA")]
    private static partial Regex MyRegex();
}