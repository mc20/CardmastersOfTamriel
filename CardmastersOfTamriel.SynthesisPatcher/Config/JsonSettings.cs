using System.Text.Json;
using System.Text.Json.Serialization;

namespace CardmastersOfTamriel.SynthesisPatcher.Config;

public static class JsonSettings
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}