using CardmastersOfTamriel.SynthesisPatcher.Models;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Services;

public class ContainerDistributorService : ILootDistributorService
{
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _skyrimMod;
    private readonly string _configFilePath;

    public ContainerDistributorService(IPatcherState<ISkyrimMod, ISkyrimModGetter> state,
        ISkyrimMod customMod, string filePathToContainerConfig)
    {
        _skyrimMod = customMod;
        _state = state;
        _configFilePath = filePathToContainerConfig;
    }

    public void DistributeLeveledItem(ICollector collector, LeveledItem collectorLeveledItem)
    {
        LeveledItemDistributorHelper.DistributeItems(_skyrimMod,
            _configFilePath,
            collector,
            collectorLeveledItem,
            AddLeveledItemToContainer);
    }

    private bool AddLeveledItemToContainer(ISkyrimMod customMod, LeveledItem leveledItem, string editorId)
    {
        Log.Verbose($"Adding LeveledItem: {leveledItem.EditorID} to Container: {editorId}.");

        var existing = _state.LoadOrder.PriorityOrder.Container().WinningOverrides()
            .FirstOrDefault(ll => ll.EditorID == editorId);
        if (existing is not null)
        {
            var modifiedContainer = customMod.Containers.GetOrAddAsOverride(existing);
            modifiedContainer.Items ??= [];

            var entry = new ContainerEntry
            {
                Item = new ContainerItem
                {
                    Item = leveledItem.ToLink(),
                    Count = 1,
                }
            };

            modifiedContainer.Items.Add(entry);
            return true;
        }

        return false;
    }
}