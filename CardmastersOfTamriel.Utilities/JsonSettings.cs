using System.Text.Json;
using System.Text.Json.Serialization;

namespace CardmastersOfTamriel.Utilities;

public static class JsonSettings
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase), 
            new FormKeyJsonConverter()
        }
    };
}