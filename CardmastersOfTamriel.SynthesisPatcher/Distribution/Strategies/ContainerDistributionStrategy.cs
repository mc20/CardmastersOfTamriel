using CardmastersOfTamriel.SynthesisPatcher.Common.Distribution.Configuration;
using CardmastersOfTamriel.SynthesisPatcher.LeveledItems;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Distribution.Strategies;

public class ContainerDistributionStrategy : ICardDistributionStrategy
{
    private readonly ISkyrimMod _customMod;
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;

    public ContainerDistributionStrategy(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod, DistributionConfiguration configuration)
    {
        _state = state;
        _customMod = customMod;
        Configuration = configuration;
    }

    public DistributionConfiguration Configuration { get; set; }

    public void DistributeToTarget(LeveledItem cardTierLeveledItem, string targetEditorId)
    {
        var recordGetter = _state.LoadOrder.PriorityOrder.Container().WinningOverrides()
            .FirstOrDefault(l => l.EditorID?.Equals(targetEditorId, StringComparison.OrdinalIgnoreCase) == true);
        if (recordGetter is null)
        {
            Log.Warning($"Target Container '{targetEditorId}' not found in load order.");
            return;
        }

        var recordForModification = _customMod.Containers.GetOrAddAsOverride(recordGetter);
        ContainerItemsBuilder.AddEntries(recordForModification, cardTierLeveledItem, 1, 1);
    }
}