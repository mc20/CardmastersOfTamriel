using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Models;

public class CollectorTypeConfiguration
{
    public required string Category { get; set; }
    public HashSet<CollectorConfig> CollectorTypes { get; set; } = [];
}

public class CollectorConfig
{
    public CollectorType Type { get; set; }
    public double ChanceNone { get; set; }
    public HashSet<TierProbabilityConfig> CardTierProbabilities { get; set; } = [];
}

public class TierProbabilityConfig
{
    public CardTier Tier { get; set; }
    public int NumberOfTimes { get; set; }
    public double ChanceNone { get; set; }
}