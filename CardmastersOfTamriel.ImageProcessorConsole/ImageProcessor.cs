using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public partial class ImageProcessor(AppConfig appConfig, MasterMetadata masterMetadata)
{
    public void Start()
    {
        EnsureDirectoryExists(appConfig.OutputFolderPath);

        foreach (var tierSourceFolderPath in Directory.EnumerateDirectories(appConfig.SourceFolderPath))
        {
            Logger.LogAction($"Tier Source Folder Path: '{tierSourceFolderPath}'");

            var tierDestinationFolderPath = Path.Combine(appConfig.OutputFolderPath, Path.GetFileName(tierSourceFolderPath));
            EnsureDirectoryExists(tierDestinationFolderPath);

            ProcessTierFolder(tierSourceFolderPath, tierDestinationFolderPath);
        }
        EnsureDirectoryExists(appConfig.OutputFolderPath);

        foreach (var tierSourceFolderPath in Directory.EnumerateDirectories(appConfig.SourceFolderPath))
        {
            Logger.LogAction($"Tier Source Folder Path: '{tierSourceFolderPath}'");

            var tierDestinationFolderPath = Path.Combine(appConfig.OutputFolderPath, Path.GetFileName(tierSourceFolderPath));
            EnsureDirectoryExists(tierDestinationFolderPath);

            ProcessTierFolder(tierSourceFolderPath, tierDestinationFolderPath);
        }
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (Directory.Exists(path)) return;
        Directory.CreateDirectory(path);
        Logger.LogAction($"Created directory: '{path}'");
    }

    private void ProcessTierFolder(string tierSourceFolderPath, string outputFolderPath)
    {
        Logger.LogAction($"Processing Source Tier folder: '{tierSourceFolderPath}'");

        EnsureDirectoryExists(outputFolderPath);

        var tier = Enum.Parse<CardTier>(Path.GetFileName(tierSourceFolderPath));
        foreach (var seriesSourceFolderPath in Directory.EnumerateDirectories(tierSourceFolderPath))
        {
            ProcessSeriesFolder(tier, seriesSourceFolderPath, outputFolderPath);
        }
    }

    private void ProcessSeriesFolder(CardTier tier, string seriesSourceFolderPath, string tierDestinationFolderPath)
    {
        Logger.LogAction($"Examining Source Series folder: '{seriesSourceFolderPath}'");

        var seriesId = Path.GetFileName(seriesSourceFolderPath);
        var newSeries = CreateNewSeries(seriesId, tier);
        newSeries.SourceFolderPath = seriesSourceFolderPath;
        newSeries.DestinationFolderPath = Path.Combine(tierDestinationFolderPath, seriesId);

        masterMetadata.Series ??= [];
        masterMetadata.Series.Add(newSeries);

        var seriesDestinationFolderPath = Path.Combine(tierDestinationFolderPath, seriesId);
        EnsureDirectoryExists(seriesDestinationFolderPath);

        WriteMetadataToFile();

        var uniqueFolderNameDictionary = new Dictionary<string, List<string>>();

        foreach (var setSourceFolderPath in Directory.EnumerateDirectories(seriesSourceFolderPath))
        {
            ProcessSetFolders(setSourceFolderPath, uniqueFolderNameDictionary);
        }

        foreach (var value in uniqueFolderNameDictionary)
        {
            Logger.LogAction($"Unique Folder Name: '{value.Key}'\n\t{string.Join("\n\t", value.Value)}\n");
        }

        MirrorSetFolders(newSeries, uniqueFolderNameDictionary);

        WriteMetadataToFile();
    }

    private static CardSeries CreateNewSeries(string seriesId, CardTier tier)
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

    private static CardSet CreateNewSet(string setId)
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

    private void WriteMetadataToFile()
    {
        var serializedJson = JsonSerializer.Serialize(masterMetadata, JsonSettings.Options);
        var jsonFilePath = Path.Combine(appConfig.OutputFolderPath, "master_metadata.json");
        File.WriteAllText(jsonFilePath, serializedJson);
        Logger.LogAction($"Serialized JSON written to file: '{jsonFilePath}'", LogMessageType.Verbose);
    }

    private static void ProcessSetFolders(string setSourceFolderPath, Dictionary<string, List<string>> uniqueFolderNameDictionary)
    {
        Logger.LogAction($"Processing Source Set folder: '{setSourceFolderPath}'", LogMessageType.Verbose);

        var originalFolderName = Path.GetFileName(setSourceFolderPath);
        var folderPattern = new Regex(@"^([a-zA-Z]+(?:[ _][a-zA-Z]+)*)", RegexOptions.IgnoreCase);
        var match = folderPattern.Match(originalFolderName);

        if (!match.Success) return;
        var uniqueFolderName = NormalizeName(match.Groups[1].Value);

        if (uniqueFolderNameDictionary.TryGetValue(uniqueFolderName, out var sourceSetFolderPaths))
        {
            sourceSetFolderPaths.Add(setSourceFolderPath);
        }
        else
        {
            uniqueFolderNameDictionary[uniqueFolderName] = [setSourceFolderPath];
        }
    }

    private static string FormatDisplayNameFromId(string setName)
    {
        var textInfo = CultureInfo.CurrentCulture.TextInfo;

        // Split and capitalize words
        var words = setName.Split('_').Select(word => textInfo.ToTitleCase(word.ToLower())).ToList();

        var mainText = string.Join(" ", words);
        var lastWord = words.Last();

        // Check if the last word is a number
        if (!int.TryParse(lastWord, out var numericValue)) return mainText;
        words.RemoveAt(words.Count - 1); // Remove the numeric part from the main text
        mainText = string.Join(" ", words);
        return $"{mainText} {numericValue}";
    }

    private List<Tuple<string, string>> MirrorSetFolders(CardSeries series, Dictionary<string, List<string>> uniqueFolderNameDictionary)
    {
        var mirroredSetPaths = new List<Tuple<string, string>>();
        var renamedFolders = new List<string>();

        foreach (var (uniqueFolderName, sourceSetPaths) in uniqueFolderNameDictionary)
        {
            if (sourceSetPaths.Count > 1)
            {
                // Multiple folders: rename with incremented suffixes
                for (var index = 0; index < sourceSetPaths.Count; index++)
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

                    WriteMetadataToFile();

                    if (!renamedFolders.Contains(newFolderName))
                    {
                        renamedFolders.Add(newFolderName);
                        Logger.LogAction($"Mirroring '{Path.GetFileName(folderPath)}' as '{uniqueFolderName}'", LogMessageType.Verbose);
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

                WriteMetadataToFile();

                if (!renamedFolders.Contains(uniqueFolderName))
                {
                    renamedFolders.Add(uniqueFolderName);
                    Logger.LogAction($"Mirroring '{Path.GetFileName(folderPath)}' as '{uniqueFolderName}'", LogMessageType.Verbose);
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
            Logger.LogAction($"Mirrored Set Folder: '{folderName}'{padding}\t=> '{folderPath}'");
        }

        return mirroredSetPaths;
    }

    private static string NormalizeName(string name)
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