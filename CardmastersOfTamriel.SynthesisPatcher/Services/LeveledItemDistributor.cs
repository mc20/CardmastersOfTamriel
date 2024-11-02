using CardmastersOfTamriel.SynthesisPatcher.Models;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using CardmastersOfTamriel.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace CardmastersOfTamriel.SynthesisPatcher;

public class LeveledItemDistributor : ILootDistributorService
{
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _skyrimMod;
    private readonly string _configFilePath;

    public LeveledItemDistributor(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod, string filePathToLeveledItemConfig)
    {
        _skyrimMod = customMod;
        _state = state;
        _configFilePath = filePathToLeveledItemConfig;
    }

    public void DistributeLeveledItem(ICollector collector, LeveledItem collectorLeveledItem)
    {
        LeveledItemDistributorHelper.DistributeItems(
            _skyrimMod,
            _configFilePath,
            collector,
            collectorLeveledItem,
            AddLeveledItemToLeveledItem);
    }

    private bool AddLeveledItemToLeveledItem(ISkyrimMod customMod, LeveledItem leveledItem, string editorId)
    {
        Logger.LogAction($"Adding LeveledItem: {leveledItem.EditorID} to LeveledItem: {editorId}.", LogMessageType.Verbose);

        var existing = _state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides().FirstOrDefault(ll => ll.EditorID == editorId);
        if (existing is not null)
        {
            var modifiedLeveledItem = customMod.LeveledItems.GetOrAddAsOverride(existing);
            modifiedLeveledItem.Entries ??= [];

            var entry = new LeveledItemEntry
            {
                Data = new LeveledItemEntryData
                {
                    Reference = existing.ToLink(),
                    Count = 1,
                    Level = 1,
                }
            };

            modifiedLeveledItem.Entries.Add(entry);
            return true;
        }
        return false;
    }
}
