using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace CardmastersOfTamriel.SynthesisPatcher;

public class LeveledItemDistributor : ILootDistributorService
{
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _skyrimMod;
    private readonly HashSet<string> _editorIds;

    public LeveledItemDistributor(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod, AppConfig appConfig)
    {
        _skyrimMod = customMod;
        _state = state;
        _editorIds = appConfig.GetEditorIds("leveledItem");
    }

    public void DistributeLeveledItem(LeveledItem leveledItem)
    {
        foreach (var editorId in _editorIds)
        {
            var container = _state.LoadOrder.PriorityOrder.Container().WinningOverrides().FirstOrDefault(ll => ll.EditorID == editorId);
            if (container is not null)
            {
                var modifiedContainer = _skyrimMod.Containers.GetOrAddAsOverride(container);
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
            }
        }
    }
}
