using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using System.Diagnostics;
using CardmastersOfTamriel.CardAssetManager;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public class ImageProcessor(AppConfig appConfig, MasterMetadata masterMetadata)
{
    private const int MaxSampleSize = 20;

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

        var cardSeries = masterMetadata.Series?.FirstOrDefault(s => s.Id == seriesId);

        if (cardSeries != null)
        {
            cardSeries.Tier = tier;
            cardSeries.SourceFolderPath = seriesSourceFolderPath;
            cardSeries.DestinationFolderPath = Path.Combine(tierDestinationFolderPath, seriesId);
        }
        else
        {
            cardSeries = CreateNewSeries(seriesId, tier);
            cardSeries.SourceFolderPath = seriesSourceFolderPath;
            cardSeries.DestinationFolderPath = Path.Combine(tierDestinationFolderPath, seriesId);

            masterMetadata.Series ??= [];
            masterMetadata.Series.Add(cardSeries);
        }

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

        var mirroredSetPaths = MirrorSetFolders(cardSeries, uniqueFolderNameDictionary);

        if (masterMetadata?.Series is not null)
        {
            Logger.LogAction("Master Metadata Series:");

            foreach (var series in masterMetadata.Series.Where(series => series.Sets is not null))
            {
                if (series.Sets is null || series.Sets.Count == 0) continue;

                foreach (var set in series.Sets)
                {
                    var id = set.Id ?? "";
                    Logger.LogAction($"Set Id: '{id}' => Destination Folder Path: '{set.DestinationFolderPath}'");

                    ProcessSet(set);
                }
            }
        }

        WriteMetadataToFile();
    }

    public static List<string> GetRandomSample(string folderPath, int sampleSize)
    {
        var images = Directory.EnumerateFiles(folderPath, "*.*")
            .Where(file => file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg"))
            .ToList();

        var random = new Random();
        return images.OrderBy(_ => random.Next()).Take(sampleSize).ToList();
    }

    private void ProcessSet(CardSet set)
    {
        Logger.LogAction($"Processing Set: '{set.Id}' at '{set.SourceFolderPath}'");
        Logger.LogAction($"Destination Folder Path: '{set.DestinationFolderPath}'", LogMessageType.Verbose);

        if (set.Id == null) return;

        var imageExtensions = new[] { "*.png", "*.jpg", "*.jpeg" };
        var imageFiles = imageExtensions.SelectMany(ext => Directory.EnumerateFiles(set.SourceFolderPath, ext));

        var totalImageCount = imageFiles.Count();
        if (totalImageCount == 0) return;

        // Randomly select a sample of images (adjust the sample size as needed)
        var sampleSize = Math.Min(MaxSampleSize, totalImageCount);
        var random = new Random();
        var selectedImages = imageFiles.OrderBy(_ => random.Next()).Take(sampleSize).ToList();

        var filePath = Path.Combine(set.DestinationFolderPath, "cards.jsonl");

        Logger.LogAction($"Card log will be saved to '{filePath}'", LogMessageType.Verbose);

        File.Delete(filePath);

        var selectedCardIndex = 0;
        foreach (var imageInfo in imageFiles.Select((filePath, index) => new { filePath, index }))
        {
            var imageIndex = imageInfo.index + 1;
            var imageFileName = $"{set.Id}_{imageIndex:D3}.dds";

            var imageDisplayName = "";
            var imageDestinationFilePath = "";
            if (selectedImages.Contains(imageInfo.filePath))
            {
                selectedCardIndex++;
                imageDisplayName = $"{set.DisplayName} - Card #{selectedCardIndex} of {selectedImages.Count}";
                imageDestinationFilePath = Path.Combine(set.DestinationFolderPath, imageFileName);
                Logger.LogAction($"Converting Image: '{imageInfo.filePath}' to '{imageDestinationFilePath}'", LogMessageType.Verbose);

                // Convert the image to DDS format
                var imageShape = DetermineOptimalShape(imageInfo.filePath);
                ProcessImage(imageInfo.filePath, imageDestinationFilePath, imageShape);
            }
            else
            {
                imageDestinationFilePath = "";
            }

            var newCard = new Card
            {
                Id = imageFileName,
                SetId = set.Id,
                SetDisplayName = set.DisplayName,
                SeriesId = set.SeriesId,
                ImageFileName = imageFileName,
                Shape = CardShape.Portrait,
                DisplayName = imageDisplayName,
                Index = imageIndex,
                TotalCount = imageFiles.Count(),
                Description = "",
                Tier = CardTier.Tier1,
                Value = 0,
                Weight = 0,
                Keywords = [],
                SourceFilePath = imageInfo.filePath,
                DestinationFilePath = imageDestinationFilePath
            };

            set.Cards ??= [];
            set.Cards.Add(newCard);

            AppendCardToFile(newCard, filePath);

            if (!string.IsNullOrEmpty(imageDestinationFilePath))
            {
                Logger.LogAction($"Created Card: '{newCard.ImageFileName}' at '{imageDestinationFilePath}'", LogMessageType.Verbose);
            }
        }

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
            SeriesId = "",
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

    private void AppendCardToFile(Card card, string filePath)
    {
        var serializedJson = JsonSerializer.Serialize(card, JsonSettings.Options);
        File.AppendAllText(filePath, serializedJson + Environment.NewLine);
        Logger.LogAction($"Serialized JSON written to file: '{filePath}'", LogMessageType.Verbose);
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
                    newSet.SeriesId = series.Id;
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
                newSet.SeriesId = series.Id;
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

        return mirroredSetPaths;
    }

    private static readonly Regex nameNormalizerRegex = new(@"[^a-z0-9_]", RegexOptions.Compiled);

    private static string NormalizeName(string name)
    {
        // Convert to lowercase
        name = name.ToLower();

        // Replace spaces with underscores
        name = name.Replace(" ", "_");

        // Remove any non-alphanumeric characters except underscores
        name = nameNormalizerRegex.Replace(name, "");

        return name;
    }

    private static void ProcessImage(string srcImagePath, string destImagePath, ImageShape shape)
    {
        using (var image = Image.Load<Rgba32>(srcImagePath))
        {
            // Perform image transformations based on shape
            switch (shape)
            {
                case ImageShape.Landscape:
                    image.Mutate(x => x.Resize(Config.TargetSizeLandscape).Rotate(90));
                    break;
                case ImageShape.Portrait:
                    image.Mutate(x => x.Resize(Config.TargetSizePortrait));
                    break;
                default: // Square
                    image.Mutate(x => x.Resize(Config.TargetSizeSquare));
                    break;
            }

            // Save as a temporary PNG file
            string tempOutputPath = Path.ChangeExtension(destImagePath, ".png");
            image.Save(tempOutputPath, new PngEncoder());

            // Convert the PNG to DDS
            FileOperations.ConvertToDDS(tempOutputPath, destImagePath);

            // Clean up temporary file
            File.Delete(tempOutputPath);
        }
    }

    private static ImageShape DetermineOptimalShape(string imagePath)
    {
        using var image = Image.Load(imagePath);
        int width = image.Width;
        int height = image.Height;

        // Calculate the retained areas for each shape
        var shapeAreas = new Dictionary<ImageShape, double>
            {
                { ImageShape.Portrait, CalculateRetainedArea(width, height, Config.TargetSizePortrait) },
                { ImageShape.Landscape, CalculateRetainedArea(width, height, Config.TargetSizeLandscape) },
                { ImageShape.Square, CalculateRetainedArea(width, height, Config.TargetSizeSquare) }
            };

        // Return the shape with the maximum retained area
        double maxArea = double.MinValue;
        ImageShape optimalShape = ImageShape.Square; // Default shape

        foreach (var shapeArea in shapeAreas)
        {
            if (shapeArea.Value > maxArea)
            {
                maxArea = shapeArea.Value;
                optimalShape = shapeArea.Key;
            }
        }

        return optimalShape;
    }

    // Method to calculate the retained area when resizing the image
    public static double CalculateRetainedArea(int originalWidth, int originalHeight, Size targetSize)
    {
        double scale = Math.Min((double)originalWidth / targetSize.Width, (double)originalHeight / targetSize.Height);
        double retainedWidth = scale * targetSize.Width;
        double retainedHeight = scale * targetSize.Height;
        return retainedWidth * retainedHeight / (originalWidth * originalHeight);
    }
}

public static class FileOperations
{
    public static void ConvertToDDS(string inputPath, string outputPath)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "texconv.exe", // Path to Texconv executable
                Arguments = $"-o {Path.GetDirectoryName(outputPath)} -ft DDS \"{inputPath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
    }
}