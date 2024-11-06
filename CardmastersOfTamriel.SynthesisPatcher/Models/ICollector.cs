using CardmastersOfTamriel.Models;
using Noggog;

namespace CardmastersOfTamriel.SynthesisPatcher.Models;

public interface ICollector
{
    CollectorType Type { get; set; }
    Percent ChanceNone { get; set; }
    List<TierProbability> CardTierProbabilities { get; set; }
}
