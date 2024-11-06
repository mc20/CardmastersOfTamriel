using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CardmastersOfTamriel.Models;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public static class FileOperations
{
    public static void ConvertToDds(string inputPath, string outputPath)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "texconv.exe"),
                Arguments = $"-o \"{Path.GetDirectoryName(outputPath)}\" -ft DDS -f DXT5 -srgb \"{inputPath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        Log.Verbose($"Converting '{Path.GetFileName(inputPath)}' to dds file: '{Path.GetFileName(outputPath)}'");

        process.OutputDataReceived += (sender, args) =>
        {
            if (string.IsNullOrEmpty(args.Data)) return;
            Log.Information(args.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.WaitForExit();
    }

    public static void AppendCardToFile(Card card, string filePath)
    {
        var serializedJson = JsonSerializer.Serialize(card, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        });
        File.AppendAllText(filePath, serializedJson + Environment.NewLine);
        Log.Verbose($"Serialized JSON written to file: '{filePath}'");
    }

    public static void EnsureDirectoryExists(string path)
    {
        if (Directory.Exists(path)) return;
        Directory.CreateDirectory(path);
        Log.Information($"Created directory: '{path}'");
    }
}