using System.Collections.Concurrent;
using System.Text.Json;
using Serilog;

namespace CardmastersOfTamriel.Utilities;

public static class JsonFileReader
{
    public static readonly ConcurrentDictionary<string, string> InvalidJsonFilesDictionary = new();

    public static async Task<T> ReadFromJsonAsync<T>(string relativeFilePath, CancellationToken cancellationToken)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var filePath = Path.Combine(baseDirectory, relativeFilePath);

        if (!File.Exists(filePath))
        {
            var fileNotFoundException = new FileNotFoundException($"The file {filePath} does not exist.");
            Log.Error(fileNotFoundException, $"Could not find file at '{relativeFilePath}'.");
            throw fileNotFoundException;
        }

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        try
        {
            var data = await JsonSerializer.DeserializeAsync<T>(fileStream, JsonSettings.Options, cancellationToken);
            if (data is not null) return data;
        }
        catch (JsonException jsonEx)
        {
            Log.Error(jsonEx, "JSON validation failed for '{FilePath}': {Message}", filePath, jsonEx.Message);
            InvalidJsonFilesDictionary.TryAdd(filePath, jsonEx.Message);
            throw new InvalidOperationException($"Invalid JSON structure in {filePath}", jsonEx);
        }

        throw new InvalidOperationException($"Failed to deserialize JSON from {filePath}");
    }

    public static async Task<List<T>> LoadAllFromJsonLineFileAsync<T>(string jsonlFilePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(jsonlFilePath))
        {
            Log.Error($"No file found at path: '{jsonlFilePath}'");
            return [];
        }

        var result = new List<T>();
        var lineNumber = 0;

        try
        {
            await using var fileStream = new FileStream(jsonlFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            using var reader = new StreamReader(fileStream);

            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    // Validate JSON structure first
                    using var document = JsonDocument.Parse(line);

                    // If valid, deserialize
                    var item = JsonSerializer.Deserialize<T>(line, JsonSettings.Options);
                    if (item is not null) result.Add(item);
                }
                catch (JsonException je)
                {
                    Log.Error(je, $"Invalid JSON at line {lineNumber} in '{jsonlFilePath}': {je.Message}");
                    InvalidJsonFilesDictionary.TryAdd(jsonlFilePath, je.Message);
                    // throw new InvalidOperationException($"Invalid JSON at line {lineNumber} in '{jsonlFilePath}'", je);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Failed to process line {lineNumber} from '{jsonlFilePath}'");
                    InvalidJsonFilesDictionary.TryAdd(jsonlFilePath, e.Message);
                    // throw new InvalidOperationException($"Failed to process line {lineNumber} from '{jsonlFilePath}'", e);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, $"Failed to read file: {jsonlFilePath}");
            throw;
        }

        return result;
    }
}