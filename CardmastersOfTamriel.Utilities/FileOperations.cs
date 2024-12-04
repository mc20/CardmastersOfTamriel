using Serilog;

namespace CardmastersOfTamriel.Utilities;

public static class FileOperations
{
    /// <summary>
    /// Ensures that the directory at the specified path exists.
    /// If the directory does not exist, it will be created.
    /// </summary>
    /// <param name="path">The path of the directory to check or create.</param>
    /// <exception cref="ArgumentException">Thrown when the provided path is null or empty.</exception>
    /// <exception cref="Exception">Thrown when the directory creation fails.</exception>
    public static void EnsureDirectoryExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Log.Error("The provided path is null or empty.");
            throw new ArgumentException("The provided path is null or empty.", nameof(path));
        }

        try
        {
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
            Log.Debug($"Created directory: '{path}'");
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to create directory");
            throw;
        }
    }
}