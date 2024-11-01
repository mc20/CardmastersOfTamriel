using CardmastersOfTamriel.Models;
using Noggog;

namespace CardmastersOfTamriel.SynthesisPatcher.Models;

public interface ITierProbability
{
    CardTier Tier { get; set; }
    int NumberOfTimes { get; set; }
    Percent ChanceNone { get; set; }
}