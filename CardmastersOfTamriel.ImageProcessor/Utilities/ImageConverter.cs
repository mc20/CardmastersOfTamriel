using CardmastersOfTamriel.Models;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public class ImageConverter
{
    private readonly Config _config;

    public ImageConverter(Config config)
    {
        _config = config;
        RegisterCleanupHandlers(); // Register the cleanup handlers
    }

    private static readonly List<string> TempFiles = [];

    public CardShape ConvertImageAndSaveToDestination(string srcImagePath, string destImagePath)
    {
        Log.Verbose($"Converting image from '{srcImagePath}' to DDS format at '{destImagePath}'");
        var imageShape = ImageHelper.DetermineOptimalShape(_config, srcImagePath);

        using var image = Image.Load<Rgba32>(srcImagePath);

        string? templateImagePath;

        // Perform image transformations based on shape
        switch (imageShape)
        {
            case CardShape.Landscape:
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = _config.ImageProperties.TargetSizes.Landscape.ToImageSharpSize(),
                    Mode = ResizeMode.Crop
                }).Rotate(-90));
                templateImagePath = _config.Paths.TemplateFiles.Landscape;
                break;
            case CardShape.Portrait:
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = _config.ImageProperties.TargetSizes.Portrait.ToImageSharpSize(),
                    Mode = ResizeMode.Crop
                }));
                templateImagePath = _config.Paths.TemplateFiles.Portrait;
                break;
            case CardShape.Square:
            default: // Square
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = _config.ImageProperties.TargetSizes.Square.ToImageSharpSize(),
                    Mode = ResizeMode.Crop
                }));
                templateImagePath = _config.Paths.TemplateFiles.Square;
                break;
        }

        using var template = Image.Load<Rgba32>(templateImagePath);
        using var templateCopy = template.Clone();

        // Superimpose the image onto the template copy
        templateCopy.Mutate(x => x.DrawImage(image, _config.ImageProperties.Offset.ToImageSharpPoint(), 1f));

        ImageHelper.ResizeImageToHeight(templateCopy, _config.ImageProperties.MaximumTextureHeight);

        // Save as a temporary PNG file
        var tempOutputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");
        TempFiles.Add(tempOutputPath); // Track the temp file for cleanup
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
                Log.Verbose($"Moved generated DDS file to '{destImagePath}'");
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

        return imageShape;
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
        foreach (var tempFile in TempFiles.Where(File.Exists))
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

        TempFiles.Clear();
    }
}