using CardmastersOfTamriel.SynthesisPatcher.Common.Distribution.Configuration;
using Mutagen.Bethesda.Skyrim;

namespace CardmastersOfTamriel.SynthesisPatcher.Distribution.Strategies;

/// <summary>
/// Defines a strategy for distributing cards to leveled lists in the game.
/// </summary>
/// <remarks>
/// Distribution strategies determine how cards are added to specific leveled lists,
/// allowing for different distribution patterns and rules.
/// </remarks>
public interface ICardDistributionStrategy
{
    DistributionConfiguration Configuration { get; set; }
    void DistributeToTarget(LeveledItem cardTierLeveledItem, string targetEditorId);
}
