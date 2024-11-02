using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public class MyProcessor
{
    private readonly AppConfig appConfig;
    private readonly MasterMetadata masterMetadata;

    internal MyProcessor(AppConfig appConfig, MasterMetadata masterMetadata)
    {
        this.appConfig = appConfig;
        this.masterMetadata = masterMetadata;
    }

    public void Start()
    {
        EnsureDirectoryExists(appConfig.OutputFolderPath);

        foreach (var tierSourceFolderPath in Directory.EnumerateDirectories(appConfig.SourceFolderPath))
        {
            DebugTools.LogAction($"Tier Source Folder Path: '{tierSourceFolderPath}'");

            var tierDestinationFolderPath = Path.Combine(appConfig.OutputFolderPath, Path.GetFileName(tierSourceFolderPath));
            EnsureDirectoryExists(tierDestinationFolderPath);

            ProcessTierFolder(tierSourceFolderPath, tierDestinationFolderPath);
        }
        EnsureDirectoryExists(appConfig.OutputFolderPath);

        foreach (var tierSourceFolderPath in Directory.EnumerateDirectories(appConfig.SourceFolderPath))
        {
            DebugTools.LogAction($"Tier Source Folder Path: '{tierSourceFolderPath}'");

            var tierDestinationFolderPath = Path.Combine(appConfig.OutputFolderPath, Path.GetFileName(tierSourceFolderPath));
            EnsureDirectoryExists(tierDestinationFolderPath);

            ProcessTierFolder(tierSourceFolderPath, tierDestinationFolderPath);
        }
    }

    public void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            DebugTools.LogAction($"Created directory: '{path}'");
        }
    }
    public void ProcessTierFolder(string tierSourceFolderPath, string outputFolderPath)
    {
        DebugTools.LogAction($"Processing Source Tier folder: '{tierSourceFolderPath}'");

        EnsureDirectoryExists(outputFolderPath);

        var tier = Enum.Parse<CardTier>(Path.GetFileName(tierSourceFolderPath));
        foreach (var seriesSourceFolderPath in Directory.EnumerateDirectories(tierSourceFolderPath))
        {
            ProcessSeriesFolder(tier, seriesSourceFolderPath, outputFolderPath);
        }
    }

    public void ProcessSeriesFolder(CardTier tier, string seriesSourceFolderPath, string tierDestinationFolderPath)
    {
        DebugTools.LogAction($"Examining Source Series folder: '{seriesSourceFolderPath}'");

        var seriesId = Path.GetFileName(seriesSourceFolderPath);
        var newSeries = CreateNewSeries(seriesId, tier);
        newSeries.SourceFolderPath = seriesSourceFolderPath;
        newSeries.DestinationFolderPath = Path.Combine(tierDestinationFolderPath, seriesId);

        var serializedJson = JsonSerializer.Serialize(newSeries, new JsonSerializerOptions { WriteIndented = true });

        var seriesDestinationFolderPath = Path.Combine(tierDestinationFolderPath, seriesId);
        EnsureDirectoryExists(seriesDestinationFolderPath);

        WriteJsonToFile(serializedJson);

        var uniqueFolderNameDictionary = new Dictionary<string, List<string>>();

        foreach (var setSourceFolderPath in Directory.EnumerateDirectories(seriesSourceFolderPath))
        {
            ProcessSetFolders(setSourceFolderPath, uniqueFolderNameDictionary);
        }

        foreach (var value in uniqueFolderNameDictionary)
        {
            DebugTools.LogAction($"Unique Folder Name: '{value.Key}'\n\t{string.Join("\n\t", value.Value)}\n");
        }

        MirrorSetFolders(newSeries, uniqueFolderNameDictionary);

        serializedJson = JsonSerializer.Serialize(newSeries, new JsonSerializerOptions { WriteIndented = true });
        WriteJsonToFile(serializedJson);
    }

    public CardSeries CreateNewSeries(string seriesId, CardTier tier)
    {
        return new CardSeries
        {
            Id = seriesId,
            Tier = tier,
            DisplayName = FormatDisplayNameFromId(seriesId),
            Theme = "",
            ReleaseDate = DateTime.UtcNow,
            Artist = "",
            IsLimitedEdition = false,
            Description = "",
            Sets = [],
            SourceFolderPath = "",
            DestinationFolderPath = ""
        };
    }

    public CardSet CreateNewSet(string setId)
    {
        return new CardSet
        {
            Id = setId,
            DisplayName = FormatDisplayNameFromId(setId),
            Theme = "",
            ReleaseDate = DateTime.UtcNow,
            Artist = "",
            IsLimitedEdition = false,
            Description = "",
            Cards = [],
            CollectorsNote = "",
            Region = "",
            ExtraAttributes = new Dictionary<string, object>(),
            SourceFolderPath = "",
            DestinationFolderPath = ""
        };
    }

    public void WriteJsonToFile(string json)
    {
        var jsonFilePath = Path.Combine(appConfig.OutputFolderPath ?? "", "master_metadata.json");
        File.WriteAllText(jsonFilePath, json);
        DebugTools.LogAction($"Serialized JSON written to file: '{jsonFilePath}'", LogMessageType.VERBOSE);
    }

    public void ProcessSetFolders(string setSourceFolderPath, Dictionary<string, List<string>> uniqueFolderNameDictionary)
    {
        DebugTools.LogAction($"Processing Source Set folder: '{setSourceFolderPath}'", LogMessageType.VERBOSE);

        var originalFolderName = Path.GetFileName(setSourceFolderPath);
        var folderPattern = new Regex(@"^([a-zA-Z]+(?:[ _][a-zA-Z]+)*)", RegexOptions.IgnoreCase);
        var match = folderPattern.Match(originalFolderName);

        if (match.Success)
        {
            var uniqueFolderName = Helpers.NormalizeName(match.Groups[1].Value);

            if (uniqueFolderNameDictionary.TryGetValue(uniqueFolderName, out List<string>? sourceSetFolderPaths))
            {
                sourceSetFolderPaths.Add(setSourceFolderPath);
            }
            else
            {
                uniqueFolderNameDictionary[uniqueFolderName] = new List<string> { setSourceFolderPath };
            }
        }
    }

    public string FormatDisplayNameFromId(string setName)
    {
        var textInfo = CultureInfo.CurrentCulture.TextInfo;

        // Split and capitalize words
        var words = setName.Split('_').Select(word => textInfo.ToTitleCase(word.ToLower())).ToList();

        string mainText = string.Join(" ", words);
        string lastWord = words.Last();

        // Check if the last word is a number
        if (int.TryParse(lastWord, out int numericValue))
        {
            words.RemoveAt(words.Count - 1); // Remove the numeric part from the main text
            mainText = string.Join(" ", words);
            return $"{mainText} {numericValue}";
        }

        return mainText;
    }

    public List<Tuple<string, string>> MirrorSetFolders(CardSeries series, Dictionary<string, List<string>> uniqueFolderNameDictionary)
    {
        var mirroredSetPaths = new List<Tuple<string, string>>();
        var renamedFolders = new List<string>();

        foreach (var entry in uniqueFolderNameDictionary)
        {
            var uniqueFolderName = entry.Key;
            var sourceSetPaths = entry.Value;

            if (sourceSetPaths.Count > 1)
            {
                // Multiple folders: rename with incremented suffixes
                for (int index = 0; index < sourceSetPaths.Count; index++)
                {
                    var folderPath = sourceSetPaths[index];
                    var newFolderName = $"{uniqueFolderName}_{index + 1:D2}";
                    var newFolderPath = Path.Combine(series.DestinationFolderPath, newFolderName);

                    Directory.CreateDirectory(newFolderPath);

                    var newSet = CreateNewSet(newFolderName);
                    newSet.Tier = series.Tier;
                    newSet.SourceFolderPath = folderPath;
                    newSet.DestinationFolderPath = newFolderPath;

                    series.Sets ??= [];
                    series.Sets.Add(newSet);

                    var serializedJson = JsonSerializer.Serialize(series, new JsonSerializerOptions { WriteIndented = true });
                    WriteJsonToFile(serializedJson);

                    if (!renamedFolders.Contains(newFolderName))
                    {
                        renamedFolders.Add(newFolderName);
                        DebugTools.LogAction($"Mirroring '{Path.GetFileName(folderPath)}' as '{uniqueFolderName}'", LogMessageType.VERBOSE);
                    }

                    mirroredSetPaths.Add(Tuple.Create(newFolderName, newFolderPath));
                }
            }
            else
            {
                // Single folder: rename if necessary, without suffix
                var folderPath = sourceSetPaths[0];
                var newFolderPath = Path.Combine(series.DestinationFolderPath, uniqueFolderName);

                Directory.CreateDirectory(newFolderPath);

                var newSet = CreateNewSet(uniqueFolderName);
                newSet.Tier = series.Tier;
                newSet.SourceFolderPath = folderPath;
                newSet.DestinationFolderPath = newFolderPath;

                series.Sets ??= [];
                series.Sets.Add(newSet);

                var serializedJson = JsonSerializer.Serialize(series, new JsonSerializerOptions { WriteIndented = true });
                WriteJsonToFile(serializedJson);

                if (!renamedFolders.Contains(uniqueFolderName))
                {
                    renamedFolders.Add(uniqueFolderName);
                    DebugTools.LogAction($"Mirroring '{Path.GetFileName(folderPath)}' as '{uniqueFolderName}'", LogMessageType.VERBOSE);
                }

                mirroredSetPaths.Add(Tuple.Create(uniqueFolderName, newFolderPath));
            }
        }

        var maxFolderNameLength = 0;
        foreach (var tuple in mirroredSetPaths)
        {
            var (folderName, folderPath) = tuple;
            maxFolderNameLength = Math.Max(maxFolderNameLength, folderName.Length);
        }

        foreach (var tuple in mirroredSetPaths)
        {
            var (folderName, folderPath) = tuple;
            var padding = new string(' ', maxFolderNameLength - folderName.Length);
            DebugTools.LogAction($"Mirrored Set Folder: '{folderName}'{padding}\t=> '{folderPath}'");
        }

        return mirroredSetPaths;
    }
    public static string NormalizeName(string name)
    {
        // Convert to lowercase
        name = name.ToLower();

        // Replace spaces with underscores
        name = name.Replace(" ", "_");

        // Remove any non-alphanumeric characters except underscores
        name = MyRegex().Replace(name, "");

        return name;
    }

    [GeneratedRegex(@"[^a-z0-9_]")]
    private static partial Regex MyRegex();
}