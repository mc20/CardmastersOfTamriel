using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.Models;
using SixLabors.ImageSharp;

namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public static class CardShapeHelper
{
    public static CardShape DetermineOptimalShape(string imagePath)
    {
        var config = ConfigurationProvider.Instance.Config;

        using var image = Image.Load(imagePath);
        var width = image.Width;
        var height = image.Height;

        // Calculate the retained areas for each shape
        var shapeAreas = new Dictionary<CardShape, double>
        {
            { CardShape.Portrait, CalculateRetainedArea(width, height, config.ImageProperties.TargetSizes.Portrait) },
            { CardShape.Landscape, CalculateRetainedArea(width, height, config.ImageProperties.TargetSizes.Landscape) },
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
    private static double CalculateRetainedArea(int originalWidth, int originalHeight, Size targetSize)
    {
        var scale = Math.Min((double)originalWidth / targetSize.Width, (double)originalHeight / targetSize.Height);
        var retainedWidth = scale * targetSize.Width;
        var retainedHeight = scale * targetSize.Height;
        return retainedWidth * retainedHeight / (originalWidth * originalHeight);
    }
}
