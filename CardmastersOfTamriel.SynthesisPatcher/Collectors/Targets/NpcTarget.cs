using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace CardmastersOfTamriel.SynthesisPatcher.Collectors.Targets;

public class NpcTarget : ICollectorTarget
{
    private readonly LeveledItem _leveledItem;

    public NpcTarget(LeveledItem leveledItem)
    {
        _leveledItem = leveledItem;
    }

    public string Name => "NPC";

    public void AddItem(IFormLink<IItemGetter> item)
    {
        _leveledItem.Entries ??= [];
        _leveledItem.Entries.Add(new LeveledItemEntry
        {
            Data = new LeveledItemEntryData
            {
                Reference = item,
                Count = 1,
                Level = 1
            }
        });
    }
}