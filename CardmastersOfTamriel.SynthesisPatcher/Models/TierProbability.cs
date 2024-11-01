using CardmastersOfTamriel.Models;
using Noggog;

namespace CardmastersOfTamriel.SynthesisPatcher.Models;

public class TierProbability : ITierProbability
{
    public CardTier Tier { get; set; }
    public int NumberOfTimes { get; set; }
    public Percent ChanceNone { get; set; }
}