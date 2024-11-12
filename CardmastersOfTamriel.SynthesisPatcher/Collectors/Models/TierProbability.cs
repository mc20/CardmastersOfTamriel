using CardmastersOfTamriel.Models;
using Noggog;

namespace CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Models;

public class TierProbability
{
    public CardTier Tier { get; set; }
    public int NumberOfTimes { get; set; }
    public Percent ChanceNone { get; set; }
}