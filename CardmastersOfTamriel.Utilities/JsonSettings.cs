using System.Text.Json;
using System.Text.Json.Serialization;

namespace CardmastersOfTamriel.Utilities;

public static class JsonSettings
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new FormKeyJsonConverter()
        }
    };

    public static readonly JsonSerializerOptions OptionsJsonl = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new FormKeyJsonConverter()
        }
    };
}