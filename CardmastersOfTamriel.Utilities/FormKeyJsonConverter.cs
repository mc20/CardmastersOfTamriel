using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mutagen.Bethesda.Plugins;

namespace CardmastersOfTamriel.Utilities;

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
            NumberStyles.HexNumber,
            null, out var id)
            ? FormKey.Null
            : new FormKey(modKey, id);
    }

    public override void Write(Utf8JsonWriter writer, FormKey value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"{value.ModKey}|0x{value.ID:X6}");
    }
}