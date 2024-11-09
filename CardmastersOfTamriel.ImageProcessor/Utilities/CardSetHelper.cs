using System.Text.RegularExpressions;
using CardmastersOfTamriel.ImageProcessor.Providers;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public static partial class CardSetHelper
{
    public static void GroupAndNormalizeFolderNames(string setSourceFolderPath,
        Dictionary<string, List<string>> groupedFolders)
    {
        var originalFolderName = Path.GetFileName(setSourceFolderPath);
        var folderPattern = MyRegex();
        var match = folderPattern.Match(originalFolderName);

        if (!match.Success) return;
        var uniqueFolderName = NameHelper.NormalizeName(match.Groups[1].Value);

        Log.Verbose($"Normalized name of '{setSourceFolderPath}' determined to be '{uniqueFolderName}'");

        if (groupedFolders.TryGetValue(uniqueFolderName, out var sourceSetFolderPaths))
        {
            sourceSetFolderPaths.Add(setSourceFolderPath);
        }
        else
        {
            groupedFolders[uniqueFolderName] = [setSourceFolderPath];
        }
    }

    [GeneratedRegex(@"^([a-zA-Z]+(?:[ _][a-zA-Z]+)*)", RegexOptions.IgnoreCase, "en-CA")]
    private static partial Regex MyRegex();

    public static string? FindMetadataLineBySetId(string setId)
    {
        var csvPath = ConfigurationProvider.Instance.Config.Paths.SetMetadataOverrideFilePath;
        if (File.Exists(csvPath))
        {
            return File.ReadLines(csvPath)
                .FirstOrDefault(line =>
                {
                    var columns = line.Split(',');
                    return columns.Length > 0 && columns[0].Trim() == setId;
                });
        }
        return null;
    }
}
