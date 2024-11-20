using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public static class FileOperations
{
    public static void EnsureDirectoryExists(string path)
    {
        try
        {
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
            Log.Verbose($"Created directory: '{path}'");
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to create directory");
            throw;
        }
    }
}
