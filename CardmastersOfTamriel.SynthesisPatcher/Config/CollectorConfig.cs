using System.Text.Json.Serialization;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Models;

namespace CardmastersOfTamriel.SynthesisPatcher.Config;

public class CollectorConfigRoot
{
    [JsonPropertyName("Collectors")]
    public List<CollectorConfig> Collectors { get; set; } = [];
}

public class CollectorConfig
{
    public CollectorType Type { get; set; }
    public double ChanceNone { get; set; }
    public List<TierProbabilityConfig> CardTierProbabilities { get; set; } = [];
}

public class TierProbabilityConfig
{
    public CardTier Tier { get; set; }
    public int NumberOfTimes { get; set; }
    public double ChanceNone { get; set; }
}