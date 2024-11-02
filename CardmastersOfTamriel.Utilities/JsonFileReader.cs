using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CardmastersOfTamriel.Utilities;

public static class JsonFileReader
{
    public static T ReadFromJson<T>(string relativeFilePath)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var filePath = Path.Combine(baseDirectory, relativeFilePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file {filePath} does not exist.");
        }

        var jsonString = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
        var data = JsonSerializer.Deserialize<T>(jsonString, options);

        return data ?? throw new InvalidOperationException($"Failed to deserialize JSON from {filePath}");
    }
}