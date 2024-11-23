using CardmastersOfTamriel.SynthesisPatcher.Common.Distribution.Configuration;
using CardmastersOfTamriel.SynthesisPatcher.LeveledItems;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Distribution.Strategies;

public class LeveledItemDistributionStrategy : ICardDistributionStrategy
{
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _customMod;
    public DistributionConfiguration Configuration { get; set; }

    public LeveledItemDistributionStrategy(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod, DistributionConfiguration configuration)
    {
        _state = state;
        _customMod = customMod;
        Configuration = configuration;
    }

    public void DistributeToTarget(LeveledItem cardTierLeveledItem, string targetEditorId)
    {
        var recordGetter = _state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides()
            .FirstOrDefault(l => l.EditorID?.Equals(targetEditorId, StringComparison.OrdinalIgnoreCase) == true);
        if (recordGetter is null)
        {
            Log.Warning($"Target LeveledItem '{targetEditorId}' not found in load order.");
            return;
        }

        var recordForModification = _customMod.LeveledItems.GetOrAddAsOverride(recordGetter);
        LeveledItemEntryBuilder.AddEntries(recordForModification, cardTierLeveledItem, 1, 1);
    }
}
