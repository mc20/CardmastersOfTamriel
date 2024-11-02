using System.Text.RegularExpressions;

public class FolderProcessor
{
    public static List<Tuple<string, string>> MirrorAndRenameSetFolders(
        string destinationSeriesPath,
        string sourceSeriesPath,
        List<string> renamedFolders)
    {
        // Dictionary to group folders by normalized names
        var uniquePaths = new Dictionary<string, List<string>>();

        // Iterate through each directory in the source series path
        foreach (var sourceSetPath in Directory.EnumerateDirectories(sourceSeriesPath))
        {
            var folderName = Path.GetFileName(sourceSetPath);
            var folderPattern = new Regex(@"^([a-zA-Z]+(?:[ _][a-zA-Z]+)*)", RegexOptions.IgnoreCase);
            var match = folderPattern.Match(folderName);

            if (match.Success)
            {
                var uniqueFolderName = Helpers.NormalizeName(match.Groups[1].Value);

                if (!uniquePaths.ContainsKey(uniqueFolderName))
                {
                    uniquePaths[uniqueFolderName] = new List<string>();
                }

                uniquePaths[uniqueFolderName].Add(sourceSetPath);
            }
        }

        var mirroredSetPaths = new List<Tuple<string, string>>();

        foreach (var entry in uniquePaths)
        {
            var uniqueFolderName = entry.Key;
            var sourceSetPaths = entry.Value;

            if (sourceSetPaths.Count > 1)
            {
                // Multiple folders: rename with incremented suffixes
                for (int index = 0; index < sourceSetPaths.Count; index++)
                {
                    var folderPath = sourceSetPaths[index];
                    var newFolderName = $"{uniqueFolderName}_{(index + 1):D2}";
                    var newFolderPath = Path.Combine(destinationSeriesPath, newFolderName);

                    Directory.CreateDirectory(newFolderPath);

                    if (!renamedFolders.Contains(newFolderName))
                    {
                        renamedFolders.Add(newFolderName);
                        LogAction($"Mirroring '{Path.GetFileName(folderPath)}' as '{newFolderName}'");
                    }

                    mirroredSetPaths.Add(Tuple.Create(newFolderName, newFolderPath));
                }
            }
            else
            {
                // Single folder: rename if necessary, without suffix
                var folderPath = sourceSetPaths[0];
                var newFolderPath = Path.Combine(destinationSeriesPath, uniqueFolderName);

                Directory.CreateDirectory(newFolderPath);

                if (!renamedFolders.Contains(uniqueFolderName))
                {
                    renamedFolders.Add(uniqueFolderName);
                    LogAction($"Mirroring '{Path.GetFileName(folderPath)}' as '{uniqueFolderName}'");
                }

                mirroredSetPaths.Add(Tuple.Create(uniqueFolderName, newFolderPath));
            }
        }

        return mirroredSetPaths;
    }

    private static void LogAction(string message)
    {
        // Logging implementation
        Console.WriteLine(message);
    }
}