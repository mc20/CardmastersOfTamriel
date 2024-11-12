using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;

namespace CardmastersOfTamriel.SynthesisPatcher.LeveledItems;


public static class LeveledItemEntryBuilder
{
    public static void AddEntries(LeveledItem entry, ILeveledItemGetter reference, short count, int numberOfTimes)
    {
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

public static class ContainerItemBuilder
{
    public static void AddEntries(Container entry, IItemGetter reference, short count, int numberOfTimes)
    {
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