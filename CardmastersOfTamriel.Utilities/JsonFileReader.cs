using System.Text.Json;
using System.Text.Json.Serialization;
using Mutagen.Bethesda.Plugins;
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