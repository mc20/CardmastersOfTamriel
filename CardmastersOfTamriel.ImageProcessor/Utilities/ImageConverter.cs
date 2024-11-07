using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.Models;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public class ImageConverter
{
    private readonly Config _config = ConfigurationProvider.Instance.Config;

    public ImageConverter()
    {
        RegisterCleanupHandlers();
    }

    private static readonly List<string> TempFiles = [];

    public CardShape ConvertImageAndSaveToDestination(string srcImagePath, string destImagePath)
    {
        Log.Verbose($"Converting image from '{srcImagePath}' to DDS format at '{destImagePath}'");
        var imageShape = ImageHelper.DetermineOptimalShape(srcImagePath);

        using var image = Image.Load<Rgba32>(srcImagePath);
        TransformImage(image, imageShape);

        using var template = LoadTemplate(imageShape);
        using var templateCopy = template.Clone();

        SuperimposeImageOntoTemplate(image, templateCopy);

        ImageHelper.ResizeImageToHeight(templateCopy, _config.ImageProperties.MaximumTextureHeight);

        var tempOutputPath = SaveAsTemporaryPng(templateCopy);
        TempFiles.Add(tempOutputPath);

        try
        {
            ConvertPngToDds(tempOutputPath, destImagePath);
        }
        finally
        {
            CleanupTemporaryFile(tempOutputPath);
        }

        return imageShape;
    }

    private void TransformImage(Image<Rgba32> image, CardShape imageShape)
    {
        switch (imageShape)
        {
            case CardShape.Landscape:
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = _config.ImageProperties.TargetSizes.Landscape.ToImageSharpSize(),
                    Mode = ResizeMode.Crop
                }).Rotate(-90));
                break;
            case CardShape.Portrait:
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = _config.ImageProperties.TargetSizes.Portrait.ToImageSharpSize(),
                    Mode = ResizeMode.Crop
                }));
                break;
            case CardShape.Square:
            default:
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = _config.ImageProperties.TargetSizes.Square.ToImageSharpSize(),
                    Mode = ResizeMode.Crop
                }));
                break;
        }
    }

    private Image<Rgba32> LoadTemplate(CardShape imageShape)
    {
        var templateImagePath = imageShape switch
        {
            CardShape.Landscape => _config.Paths.TemplateFiles.Landscape,
            CardShape.Portrait => _config.Paths.TemplateFiles.Portrait,
            _ => _config.Paths.TemplateFiles.Square
        };

        return Image.Load<Rgba32>(templateImagePath);
    }

    private void SuperimposeImageOntoTemplate(Image<Rgba32> image, Image<Rgba32> templateCopy)
    {
        templateCopy.Mutate(x => x.DrawImage(image, _config.ImageProperties.Offset.ToImageSharpPoint(), 1f));
    }

    private static string SaveAsTemporaryPng(Image<Rgba32> templateCopy)
    {
        var tempOutputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");
        templateCopy.Save(tempOutputPath, new PngEncoder());
        return tempOutputPath;
    }

    private static void ConvertPngToDds(string tempOutputPath, string destImagePath)
    {
        FileOperations.ConvertToDds(tempOutputPath, destImagePath);
        var outputDirectory = Path.GetDirectoryName(destImagePath) ?? Directory.GetCurrentDirectory();
        var generatedDdsPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(tempOutputPath) + ".dds");
        if (!File.Exists(generatedDdsPath)) return;
        
        File.Move(generatedDdsPath, destImagePath, overwrite: true);
        Log.Verbose($"Moved generated DDS file to '{destImagePath}'");
    }

    private static void CleanupTemporaryFile(string tempOutputPath)
    {
        if (!File.Exists(tempOutputPath)) return;
        
        File.Delete(tempOutputPath);
        Log.Verbose($"Deleted temporary image file at '{tempOutputPath}'");
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