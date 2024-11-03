using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public class ImageHelper
{
    private readonly AppConfig _appConfig;

    public ImageHelper(AppConfig appConfig)
    {
        _appConfig = appConfig;
    }

    public void ProcessImage(string srcImagePath, string destImagePath)
    {
        var imageShape = DetermineOptimalShape(srcImagePath);

        using var image = Image.Load<Rgba32>(srcImagePath);
        var templateImagePath = string.Empty;

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
            default: // Square
                image.Mutate(x => x.Resize(Config.TargetSizeSquare));
                templateImagePath = _appConfig.TemplateFilePathSquare;
                break;
        }

        using var template = Image.Load<Rgba32>(templateImagePath);
        using var templateCopy = template.Clone();

        // Superimpose the image onto the template copy
        templateCopy.Mutate(x => x.DrawImage(image, Config.Offset, 1f));

        // Save as a temporary PNG file
        string tempOutputPath = Path.ChangeExtension(destImagePath, ".png");
        templateCopy.Save(tempOutputPath, new PngEncoder());

        // Convert the PNG to DDS
        FileOperations.ConvertToDDS(tempOutputPath, destImagePath);

        // Clean up temporary file
        File.Delete(tempOutputPath);
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
    private static double CalculateRetainedArea(int originalWidth, int originalHeight, Size targetSize)
    {
        double scale = Math.Min((double)originalWidth / targetSize.Width, (double)originalHeight / targetSize.Height);
        double retainedWidth = scale * targetSize.Width;
        double retainedHeight = scale * targetSize.Height;
        return retainedWidth * retainedHeight / (originalWidth * originalHeight);
    }
}