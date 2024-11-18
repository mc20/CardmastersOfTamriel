using System.Text.Json;
using System.Text.Json.Serialization;
using CardmastersOfTamriel.Models;
using Mutagen.Bethesda.Plugins;
using Serilog;

namespace CardmastersOfTamriel.Utilities;

public static class JsonFileReader
{
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

    public static async Task<T> ReadFromJsonAsync<T>(string relativeFilePath, CancellationToken cancellationToken = default)
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
            var data = await JsonSerializer.DeserializeAsync<T>(fileStream, JsonSettings.Options, cancellationToken);
            if (data is not null) return data;
        }

        var invalidOperationException = new InvalidOperationException($"Failed to deserialize JSON from {filePath}");
        Log.Error(invalidOperationException, $"Failed to deserialize JSON from {filePath}.");
        throw invalidOperationException;
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
        var uniqueCards = new HashSet<string>();

        var cardsFromMetadataFile = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<T>(line, JsonSettings.Options))
            .ToHashSet();

        return cardsFromMetadataFile;
    }


    public static async Task<T?> FindMetadataLineBySetIdAsync<T>(string jsonlPath, string targetId, CancellationToken cancellationToken = default) where T : class
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

            while (await reader.ReadLineAsync() is { } line)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var item = JsonSerializer.Deserialize<T>(line);
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