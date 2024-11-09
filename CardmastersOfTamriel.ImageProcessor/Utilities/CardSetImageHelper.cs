namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public static class CardSetImageHelper
{
    public static HashSet<string> GetImageFilePathsFromFolder(string folderPath, string[]? imageExtensions = null)
    {
        imageExtensions ??= ["*.png", "*.jpg", "*.jpeg"];
        return imageExtensions.SelectMany(ext => Directory.EnumerateFiles(folderPath, ext)).ToHashSet<string>();
    }

    public static HashSet<string> SelectRandomImageFilePaths(int maxSampleSize, HashSet<string> imageFilePaths)
    {
        // Randomly select a sample of images (adjust the sample size as needed)
        var random = new Random();
        return imageFilePaths.OrderBy(_ => random.Next()).Take(maxSampleSize).ToHashSet();
    }
}