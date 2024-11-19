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

    public async Task<CardShape> ConvertImageAndSaveToDestinationAsync(CardTier cardTier, string srcImagePath, string destImagePath, CancellationToken cancellationToken)

    {
        Log.Verbose($"Converting image from '{srcImagePath}' to DDS format at '{destImagePath}'");
        var imageShape = ImageHelper.DetermineOptimalShape(srcImagePath);

        using var image = await Task.Run(() => Image.Load<Rgba32>(srcImagePath), cancellationToken);
        TransformImage(image, imageShape);

        using var template = await LoadTemplateAsync(imageShape, cardTier, cancellationToken);
        using var templateCopy = template.Clone();

        SuperimposeImageOntoTemplate(image, templateCopy);

        ImageHelper.ResizeImageToHeight(templateCopy, _config.ImageProperties.MaximumTextureHeight);

        var tempOutputPath = await SaveAsTemporaryPngAsync(templateCopy, cancellationToken);
        TempFiles.Add(tempOutputPath);

        try
        {
            await ConvertPngToDdsAsync(tempOutputPath, destImagePath, cancellationToken);
        }
        finally
        {
            await CleanupTemporaryFileAsync(tempOutputPath);
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

    private async Task<Image<Rgba32>> LoadTemplateAsync(CardShape imageShape, CardTier cardTier, CancellationToken cancellationToken)
    {
        var templateFileTier = cardTier switch
        {
            CardTier.Tier1 => _config.Paths.TemplateFiles.Tier1,
            CardTier.Tier2 => _config.Paths.TemplateFiles.Tier2,
            CardTier.Tier3 => _config.Paths.TemplateFiles.Tier3,
            CardTier.Tier4 => _config.Paths.TemplateFiles.Tier4,
            _ => throw new ArgumentException($"Unsupported card tier: {cardTier}")
        };

        var templateImagePath = imageShape switch
        {
            CardShape.Portrait => templateFileTier.Portrait,
            CardShape.Landscape => templateFileTier.Landscape,
            CardShape.Square => templateFileTier.Square,
            _ => throw new ArgumentException($"Unsupported image shape: {imageShape}")
        };

        return await Task.Run(() => Image.Load<Rgba32>(templateImagePath), cancellationToken);
    }

    private void SuperimposeImageOntoTemplate(Image<Rgba32> image, Image<Rgba32> templateCopy)
    {
        templateCopy.Mutate(x => x.DrawImage(image, _config.ImageProperties.Offset.ToImageSharpPoint(), 1f));
    }

    private static async Task<string> SaveAsTemporaryPngAsync(Image<Rgba32> templateCopy, CancellationToken cancellationToken)
    {
        var tempOutputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");
        await Task.Run(() => templateCopy.Save(tempOutputPath, new PngEncoder()), cancellationToken);
        return tempOutputPath;
    }

    private static async Task ConvertPngToDdsAsync(string tempOutputPath, string destImagePath, CancellationToken cancellationToken)
    {
        await Task.Run(() => FileOperations.ConvertToDdsAsync(tempOutputPath, destImagePath, cancellationToken), cancellationToken);

        var outputDirectory = Path.GetDirectoryName(destImagePath) ?? Directory.GetCurrentDirectory();
        var generatedDdsPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(tempOutputPath) + ".dds");
        if (!File.Exists(generatedDdsPath)) return;

        await Task.Run(() => File.Move(generatedDdsPath, destImagePath, overwrite: true), cancellationToken);
        Log.Verbose($"Moved generated DDS file to '{destImagePath}'");
    }

    private static async Task CleanupTemporaryFileAsync(string tempOutputPath)
    {
        if (!File.Exists(tempOutputPath)) return;

        await Task.Run(() => File.Delete(tempOutputPath));
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

        AppDomain.CurrentDomain.ProcessExit += (sender, e) => { CleanupTempFiles(); };
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
