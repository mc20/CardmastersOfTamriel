using CardmastersOfTamriel.SynthesisPatcher.Common.Distribution.Configuration;
using CardmastersOfTamriel.SynthesisPatcher.LeveledItems;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Distribution.Strategies;

public class NpcDistributionStrategy : ICardDistributionStrategy
{
    private readonly ISkyrimMod _customMod;
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;

    public NpcDistributionStrategy(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod, DistributionConfiguration configuration)
    {
        _state = state;
        _customMod = customMod;
        Configuration = configuration;
    }

    public DistributionConfiguration Configuration { get; set; }

    public void DistributeToTarget(LeveledItem cardTierLeveledItem, string targetEditorId)
    {
        var recordGetter = _state.LoadOrder.PriorityOrder.Npc().WinningOverrides()
            .FirstOrDefault(l => l.EditorID?.Equals(targetEditorId, StringComparison.OrdinalIgnoreCase) == true);
        if (recordGetter is null)
        {
            Log.Warning($"Target Npc '{targetEditorId}' not found in load order.");
            return;
        }

        var recordForModification = _customMod.Npcs.GetOrAddAsOverride(recordGetter);
        NpcInventoryBuilder.AddItems(recordForModification, cardTierLeveledItem, 1, 1);
    }
}