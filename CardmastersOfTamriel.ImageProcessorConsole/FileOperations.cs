using System.Diagnostics;
using System.Text.Json;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public static class FileOperations
{
    public static void ConvertToDDS(string inputPath, string outputPath)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "texconv.exe"),
                Arguments = $"-o {Path.GetDirectoryName(outputPath)} -ft DDS \"{inputPath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
    }

    public static void AppendCardToFile(Card card, string filePath)
    {
        var serializedJson = JsonSerializer.Serialize(card, JsonSettings.Options);
        File.AppendAllText(filePath, serializedJson + Environment.NewLine);
        Logger.LogAction($"Serialized JSON written to file: '{filePath}'", LogMessageType.Verbose);
    }

    public static void EnsureDirectoryExists(string path)
    {
        if (Directory.Exists(path)) return;
        Directory.CreateDirectory(path);
        Logger.LogAction($"Created directory: '{path}'");
    }
}
