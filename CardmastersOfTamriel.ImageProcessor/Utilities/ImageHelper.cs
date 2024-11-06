using CardmastersOfTamriel.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public static class ImageHelper
{
    public static CardShape DetermineOptimalShape(Config config, string imagePath)
    {
        using var image = Image.Load(imagePath);
        var width = image.Width;
        var height = image.Height;

        // Calculate the retained areas for each shape
        var shapeAreas = new Dictionary<CardShape, double>
        {
            { CardShape.Portrait, CalculateRetainedArea(width, height, config.ImageProperties.TargetSizes.Portrait) },
            {
                CardShape.Landscape, CalculateRetainedArea(width, height, config.ImageProperties.TargetSizes.Landscape)
            },
            { CardShape.Square, CalculateRetainedArea(width, height, config.ImageProperties.TargetSizes.Square) }
        };

        // Return the shape with the maximum retained area
        var maxArea = double.MinValue;
        var optimalShape = CardShape.Square; // Default shape

        foreach (var shapeArea in shapeAreas.Where(shapeArea => shapeArea.Value > maxArea))
        {
            maxArea = shapeArea.Value;
            optimalShape = shapeArea.Key;
        }

        return optimalShape;
    }
    
    // Method to calculate the retained area when resizing the image
    public static double CalculateRetainedArea(int originalWidth, int originalHeight, Size targetSize)
    {
        var scale = Math.Min((double)originalWidth / targetSize.Width, (double)originalHeight / targetSize.Height);
        var retainedWidth = scale * targetSize.Width;
        var retainedHeight = scale * targetSize.Height;
        return retainedWidth * retainedHeight / (originalWidth * originalHeight);
    }
    
    public static void ResizeImageToHeight(Image<Rgba32> image, int targetHeight)
    {
        // Calculate the new width to maintain the aspect ratio
        var originalWidth = image.Width;
        var originalHeight = image.Height;
        var targetWidth = (int)((double)originalWidth / originalHeight * targetHeight);

        // Perform the resize
        image.Mutate(x => x.Resize(targetWidth, targetHeight));
    }
}