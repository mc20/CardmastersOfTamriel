using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public static class ImageResizer
{
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