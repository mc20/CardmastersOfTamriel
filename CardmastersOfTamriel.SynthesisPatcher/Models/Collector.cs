using CardmastersOfTamriel.Models;
using Noggog;

namespace CardmastersOfTamriel.SynthesisPatcher.Models;

public class Collector(CollectorType type, Percent chanceNone, List<TierProbability> cardTierProbabilities) : ICollector
{
    public CollectorType Type { get; set; } = type;
    public Percent ChanceNone { get; set; } = chanceNone;
    public List<TierProbability> CardTierProbabilities { get; set; } = cardTierProbabilities;
}
