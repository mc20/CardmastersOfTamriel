using System.Text.Json.Serialization;

namespace CardmastersOfTamriel.SynthesisPatcher.Utilities;

public class InventoryInjectorConfig
{
    [JsonPropertyName("rules")] public List<Rule> Rules { get; set; } = new();
}

public class Rule
{
    [JsonPropertyName("match")] public Match Match { get; set; } = new();

    [JsonPropertyName("assign")] public Assign Assign { get; set; } = new();
}

public class Match
{
    [JsonPropertyName("formType")] public string FormType { get; set; } = "MiscItem";

    [JsonPropertyName("keywords")] public List<string> Keywords { get; set; } = new();
}

public class Assign
{
    [JsonPropertyName("subType")] public string SubType { get; set; } = string.Empty;

    [JsonPropertyName("subTypeDisplay")] public string SubTypeDisplay { get; set; } = string.Empty;
}