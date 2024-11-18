using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
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

    public static void AppendDataToFile<T>(T item, string filePath)
    {
        var serializedJson = JsonSerializer.Serialize(item, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        });
        File.AppendAllText(filePath, serializedJson + Environment.NewLine);
        Log.Verbose($"Serialized JSON written to file: '{filePath}'");
    }

    public static void AppendCardSetToFile(CardSet set, string filePath)
    {
        var serializedJson = JsonSerializer.Serialize(set, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        });
        File.AppendAllText(filePath, serializedJson + Environment.NewLine);
        Log.Verbose($"Serialized JSON written to file: '{filePath}'");
    }

    public static void EnsureDirectoryExists(string path)
    {
        try
        {
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
            Log.Information($"Created directory: '{path}'");
        }
        catch (Exception e)
        {
           Log.Error(e, "Failed to create directory");
            throw;
        }
    }

    [Obsolete("Use FindMetadataLineBySetIdAsync instead", false)]
    public static T? FindMetadataLineBySetId<T>(string jsonlPath, string targetId) where T : class
    {
        if (!File.Exists(jsonlPath))
        {
            Log.Error($"No file found at path: '{jsonlPath}'");
            return null;
        }

        foreach (var line in File.ReadLines(jsonlPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var item = JsonSerializer.Deserialize<T>(line, JsonSettings.Options);
                if (item is IIdentifiable identifiable && identifiable.Id == targetId)
                {
                    return item;
                }
            }
            catch (JsonException ex)
            {
                Log.Error($"Failed to parse line: {ex.Message}");
                throw;
            }
        }
        return null;
    }
    

}
