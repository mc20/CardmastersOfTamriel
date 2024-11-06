using System.Text.Json;
using Serilog;

namespace CardmastersOfTamriel.Utilities;

public static class JsonFileReader
{
    public static T ReadFromJson<T>(string relativeFilePath)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var filePath = Path.Combine(baseDirectory, relativeFilePath);

        if (!File.Exists(filePath))
        {
            var e = new FileNotFoundException($"The file {filePath} does not exist.");
            Log.Error(e, $"Could not find file at '{relativeFilePath}'.");
            throw e;
        }

        var jsonString = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<T>(jsonString, JsonSettings.Options);

        if (data is not null) return data;
        {
            var e = new InvalidOperationException($"Failed to deserialize JSON from {filePath}");
            Log.Error(e, $"Failed to deserialize JSON from {filePath}.");
            throw e;
        }
    }
}