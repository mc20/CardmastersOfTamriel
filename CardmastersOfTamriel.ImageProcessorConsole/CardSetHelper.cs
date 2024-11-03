using System.Text.RegularExpressions;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public static class CardSetHelper
{
    public static void NormalizeAndGroupSetFoldersForDestination(string setSourceFolderPath, Dictionary<string, List<string>> groupedFolders)
    {
        Logger.LogAction($"Processing Source Set folder: '{setSourceFolderPath}'", LogMessageType.Verbose);

        var originalFolderName = Path.GetFileName(setSourceFolderPath);
        var folderPattern = new Regex(@"^([a-zA-Z]+(?:[ _][a-zA-Z]+)*)", RegexOptions.IgnoreCase);
        var match = folderPattern.Match(originalFolderName);

        if (!match.Success) return;
        var uniqueFolderName = NameHelper.NormalizeName(match.Groups[1].Value);

        if (groupedFolders.TryGetValue(uniqueFolderName, out var sourceSetFolderPaths))
        {
            sourceSetFolderPaths.Add(setSourceFolderPath);
        }
        else
        {
            groupedFolders[uniqueFolderName] = [setSourceFolderPath];
        }
    }
}
