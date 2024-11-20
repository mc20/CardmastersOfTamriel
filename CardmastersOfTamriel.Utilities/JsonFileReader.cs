using System.Text.Json;
using System.Text.Json.Serialization;
using CardmastersOfTamriel.Models;
using Mutagen.Bethesda.Plugins;
using Serilog;

namespace CardmastersOfTamriel.Utilities;

public static class JsonFileReader
{
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

        await using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            try
            {
                await JsonDocument.ParseAsync(fileStream, cancellationToken: cancellationToken);
                fileStream.Position = 0;

                var data = await JsonSerializer.DeserializeAsync<T>(fileStream, JsonSettings.Options, cancellationToken);
                if (data is not null) return data;
            }
            catch (JsonException jsonEx)
            {
                var invalidJsonException = new InvalidOperationException($"Invalid JSON structure in {filePath}", jsonEx);
                Log.Error(invalidJsonException, "JSON validation failed for '{FilePath}': {Message}", filePath, jsonEx.Message);
                throw invalidJsonException;
            }
        }

        var invalidOperationException = new InvalidOperationException($"Failed to deserialize JSON from {filePath}");
        Log.Error(invalidOperationException, $"Failed to deserialize JSON from {filePath}.");
        throw invalidOperationException;
    }

    public static async Task<List<T>> LoadAllFromJsonLineFileAsync<T>(string jsonlFilePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(jsonlFilePath))
        {
            Log.Error($"No file found at path: '{jsonlFilePath}'");
            return new List<T>();
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
                    if (item is not null)
                    {
                        result.Add(item);
                    }
                }
                catch (JsonException je)
                {
                    Log.Error(je, $"Invalid JSON at line {lineNumber} in '{jsonlFilePath}': {je.Message}");
                    throw new InvalidOperationException($"Invalid JSON at line {lineNumber} in '{jsonlFilePath}'", je);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Failed to process line {lineNumber} from '{jsonlFilePath}'");
                    throw new InvalidOperationException($"Failed to process line {lineNumber} from '{jsonlFilePath}'", e);
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

    public static async Task<T?> FindMetadataLineBySetIdAsync<T>(string jsonlPath, string targetId, CancellationToken cancellationToken) where T : class
    {
        if (!File.Exists(jsonlPath))
        {
            Log.Error($"No file found at path: '{jsonlPath}'");
            return null;
        }

        try
        {
            // Open a file stream for reading asynchronously
            await using var stream = new FileStream(jsonlPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            using var reader = new StreamReader(stream);

            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    // Validate JSON structure first
                    using var document = JsonDocument.Parse(line);

                    // If valid, deserialize
                    var item = JsonSerializer.Deserialize<T>(line, JsonSettings.Options);
                    if (item is IIdentifiable identifiable && identifiable.Id == targetId)
                    {
                        return item;
                    }
                }
                catch (JsonException ex)
                {
                    Log.Error(ex, $"Failed to parse line from {jsonlPath}");
                    throw new InvalidOperationException($"Invalid JSON at line in '{jsonlPath}'", ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            Log.Information("Operation was canceled.");
            throw;
        }
        catch (IOException ex)
        {
            Log.Error($"Failed to read file: {ex.Message}");
            throw;
        }

        return null;
    }

    [Obsolete("Use ReadFromJsonAsync instead", false)]
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

    [Obsolete("Use LoadAllFromJsonLineFileAsync instead", false)]
    public static HashSet<T?> LoadAllFromJsonLineFile<T>(string jsonlFilePath)
    {
        if (!File.Exists(jsonlFilePath))
        {
            Log.Error($"No cards.jsonl file found at '{jsonlFilePath}'");
            return [];
        }

        var lines = new HashSet<string>(File.ReadLines(jsonlFilePath));

        var cardsFromMetadataFile = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<T>(line, JsonSettings.Options))
            .ToHashSet();

        return cardsFromMetadataFile;
    }
}

public class FormKeyJsonConverter : JsonConverter<FormKey>
{
    public override FormKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value)) return FormKey.Null;

        // Expects format: "ModName.esp|0x123456"
        var parts = value.Split('|');
        if (parts.Length != 2) return FormKey.Null;

        var modKey = ModKey.FromFileName(parts[0]);

        return !uint.TryParse(parts[1].Replace("0x", ""),
            System.Globalization.NumberStyles.HexNumber,
            null, out var id)
            ? FormKey.Null
            : new FormKey(modKey, id);
    }

    public override void Write(Utf8JsonWriter writer, FormKey value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"{value.ModKey}|0x{value.ID:X6}");
    }
}