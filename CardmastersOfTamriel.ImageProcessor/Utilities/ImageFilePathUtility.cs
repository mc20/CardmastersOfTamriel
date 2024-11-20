namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public static class ImageFilePathUtility
{
    /// <summary>
    /// Retrieves a set of image file paths from the specified folder.
    /// </summary>
    /// <param name="folderPath">The path to the folder to search for image files.</param>
    /// <param name="imageExtensions">
    /// An optional array of image file extensions to search for. 
    /// If not provided, defaults to searching for "*.png", "*.jpg", and "*.jpeg" files.
    /// </param>
    /// <returns>A <see cref="HashSet{T}"/> containing the file paths of the images found in the specified folder.</returns>
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