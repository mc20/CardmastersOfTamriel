using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Collectors.Targets;
using Noggog;

namespace CardmastersOfTamriel.SynthesisPatcher.Models;

public class CollectorConfig : ICollectorConfig
{
    public CollectorConfig(CollectorType type, Percent chanceNone, List<TierProbability> cardTierProbabilities, string name)
    {
        Type = type;
        ChanceNone = chanceNone;
        CardTierProbabilities = cardTierProbabilities;
        Name = name;
    }

    public string Name { get; }
    public CollectorType Type { get; set; }
    public Percent ChanceNone { get; set; }
    public List<TierProbability> CardTierProbabilities { get; set; }
    public ICollectorTarget AddNewCollectorAndGetTarget()
    {
        throw new NotImplementedException();
    }
}
