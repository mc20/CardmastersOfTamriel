using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;

namespace CardmastersOfTamriel.SynthesisPatcher;

public class ContainerDistributorService : ILootDistributorService
{
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _skyrimMod;
    private readonly HashSet<string> _editorIds;

    public ContainerDistributorService(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod, AppConfig appConfig)
    {
        _skyrimMod = customMod;
        _state = state;
        _editorIds = appConfig.GetEditorIds("containers");
    }

    public void DistributeLeveledItem(LeveledItem leveledItem)
    {
        foreach (var editorId in _editorIds)
        {
            var existingLeveledItem = _state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides().FirstOrDefault(ll => ll.EditorID == editorId);
            if (existingLeveledItem is not null)
            {
                var modifiedLeveledItem = _skyrimMod.LeveledItems.GetOrAddAsOverride(existingLeveledItem);
                modifiedLeveledItem.Entries ??= [];

                var entry = new LeveledItemEntry
                {
                    Data = new LeveledItemEntryData
                    {
                        Reference = leveledItem.ToLink(),
                        Count = 1,
                        Level = 1,
                    }
                };

                modifiedLeveledItem.Entries.Add(entry);
                modifiedLeveledItem.ChanceNone = Percent.Zero;
            }
        }
    }
}
