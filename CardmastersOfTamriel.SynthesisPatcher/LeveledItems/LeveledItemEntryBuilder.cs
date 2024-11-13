using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.LeveledItems;


public static class LeveledItemEntryBuilder
{
    public static void AddEntries(LeveledItem entry, ILeveledItemGetter reference, short count, int numberOfTimes)
    {
        Log.Debug($"LeveledItemEntryBuilder: Adding ILeveledItemGetter '{reference.EditorID}' to LeveledItem '{entry.EditorID}' {numberOfTimes} times");

        entry.Entries ??= [];
        for (var i = 0; i < numberOfTimes; i++)
        {
            entry.Entries.Add(new LeveledItemEntry()
            {
                Data = new LeveledItemEntryData()
                {
                    Reference = reference.ToLink(),
                    Count = count,
                    Level = 1
                }
            });
        }
    }
}

public static class ContainerItemsBuilder
{
    public static void AddEntries(Container entry, IItemGetter reference, short count, int numberOfTimes)
    {
        Log.Debug($"ContainerItemsBuilder: Adding IItemGetter '{reference.EditorID}' to Container '{entry.EditorID}' {numberOfTimes} times");

        entry.Items ??= [];
        for (var i = 0; i < numberOfTimes; i++)
        {
            entry.Items.Add(new ContainerEntry()
            {
                Item = new ContainerItem()
                {
                    Item = reference.ToLink(),
                    Count = count
                }
            });
        }
    }
}

public static class NpcInventoryBuilder
{
    public static void AddItems(Npc npc, IItemGetter reference, short count, int numberOfTimes)
    {
        Log.Debug($"NpcInventoryBuilder: Adding IItemGetter '{reference.EditorID}' to Npc '{npc.EditorID}' {numberOfTimes} times");

        npc.Items ??= [];
        for (var i = 0; i < numberOfTimes; i++)
        {
            npc.Items.Add(new ContainerEntry()
            {
                Item = new ContainerItem()
                {
                    Item = reference.ToLink(),
                    Count = count
                }
            });
        }
    }
}