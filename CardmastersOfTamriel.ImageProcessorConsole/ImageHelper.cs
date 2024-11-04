using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using CardmastersOfTamriel.Models;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public class ImageHelper
{
    private readonly AppConfig _appConfig;

    public ImageHelper(AppConfig appConfig)
    {
        _appConfig = appConfig;
    }

    private static List<string> _tempFiles = [];

    public void ConvertImageAndSaveToDestination(string srcImagePath, string destImagePath)
    {
        Log.Verbose($"Converting image from '{srcImagePath}' to DDS format at '{destImagePath}'");
        var imageShape = DetermineOptimalShape(srcImagePath);

        using var image = Image.Load<Rgba32>(srcImagePath);

        string? templateImagePath;

        // Perform image transformations based on shape
        switch (imageShape)
        {
            case ImageShape.Landscape:
                image.Mutate(x => x.Resize(Config.TargetSizeLandscape).Rotate(90));
                templateImagePath = _appConfig.TemplateFilePathLandscape;
                break;
            case ImageShape.Portrait:
                image.Mutate(x => x.Resize(Config.TargetSizePortrait));
                templateImagePath = _appConfig.TemplateFilePathPortrait;
                break;
            case ImageShape.Square:
            default: // Square
                image.Mutate(x => x.Resize(Config.TargetSizeSquare));
                templateImagePath = _appConfig.TemplateFilePathSquare;
                break;
        }

        if (templateImagePath == null) return;

        using var template = Image.Load<Rgba32>(templateImagePath);
        using var templateCopy = template.Clone();

        // Superimpose the image onto the template copy
        templateCopy.Mutate(x => x.DrawImage(image, Config.Offset, 1f));

        // Save as a temporary PNG file
        // var tempOutputPath = Path.ChangeExtension(destImagePath, ".png");
        var tempOutputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");
        _tempFiles.Add(tempOutputPath); // Track the temp file for cleanup
        templateCopy.Save(tempOutputPath, new PngEncoder());

        try
        {
            // Convert the PNG to DDS
            FileOperations.ConvertToDds(tempOutputPath, destImagePath);
            var outputDirectory = Path.GetDirectoryName(destImagePath) ?? Directory.GetCurrentDirectory();
            var generatedDdsPath =
                Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(tempOutputPath) + ".dds");
            if (File.Exists(generatedDdsPath))
            {
                File.Move(generatedDdsPath, destImagePath, overwrite: true);
            }
        }
        finally
        {
            // Clean up temporary file
            if (File.Exists(tempOutputPath))
            {
                File.Delete(tempOutputPath);
                Log.Verbose($"Deleted temporary image file at '{tempOutputPath}");
            }
        }
    }

    private static void RegisterCleanupHandlers()
    {
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            CleanupTempFiles();
            Environment.Exit(0);
        };

        AppDomain.CurrentDomain.ProcessExit += ((sender, e) => { CleanupTempFiles(); });
    }

    private static void CleanupTempFiles()
    {
        foreach (var tempFile in _tempFiles.Where(File.Exists))
        {
            try
            {
                File.Delete(tempFile);
                Log.Verbose($"Deleted temporary image file at '{tempFile}'");
            }
            catch (Exception e)
            {
                Log.Error($"Failed to delete temporary image file at '{tempFile}': {e.Message}");
            }
        }

        _tempFiles.Clear();
    }

    private static ImageShape DetermineOptimalShape(string imagePath)
    {
        using var image = Image.Load(imagePath);
        var width = image.Width;
        var height = image.Height;

        // Calculate the retained areas for each shape
        var shapeAreas = new Dictionary<ImageShape, double>
        {
            { ImageShape.Portrait, CalculateRetainedArea(width, height, Config.TargetSizePortrait) },
            { ImageShape.Landscape, CalculateRetainedArea(width, height, Config.TargetSizeLandscape) },
            { ImageShape.Square, CalculateRetainedArea(width, height, Config.TargetSizeSquare) }
        };

        // Return the shape with the maximum retained area
        var maxArea = double.MinValue;
        var optimalShape = ImageShape.Square; // Default shape

        foreach (var shapeArea in shapeAreas.Where(shapeArea => shapeArea.Value > maxArea))
        {
            maxArea = shapeArea.Value;
            optimalShape = shapeArea.Key;
        }

        return optimalShape;
    }

    // Method to calculate the retained area when resizing the image
    private static double CalculateRetainedArea(int originalWidth, int originalHeight, Size targetSize)
    {
        var scale = Math.Min((double)originalWidth / targetSize.Width, (double)originalHeight / targetSize.Height);
        var retainedWidth = scale * targetSize.Width;
        var retainedHeight = scale * targetSize.Height;
        return retainedWidth * retainedHeight / (originalWidth * originalHeight);
    }
}