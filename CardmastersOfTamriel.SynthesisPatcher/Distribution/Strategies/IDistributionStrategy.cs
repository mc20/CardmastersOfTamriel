using CardmastersOfTamriel.SynthesisPatcher.Distribution.Configuration;
using Mutagen.Bethesda.Skyrim;

namespace CardmastersOfTamriel.SynthesisPatcher.Distribution.Strategies;

public interface ICardDistributionStrategy
{
    DistributionConfiguration Configuration { get; set; }
    void DistributeToTarget(LeveledItem cardTierLeveledItem, string targetEditorId);
}
