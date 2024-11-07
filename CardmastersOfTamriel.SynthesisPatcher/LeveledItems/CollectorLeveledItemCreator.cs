using CardmastersOfTamriel.SynthesisPatcher.Diagnostics;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace CardmastersOfTamriel.SynthesisPatcher.LeveledItems;

public class CollectorLeveledItemCreator
{
    private const int MaxEntriesPerLeveledList = 100;
    private readonly ISkyrimMod _skyrimMod;

    public CollectorLeveledItemCreator(ISkyrimMod skyrimMod)
    {
        _skyrimMod = skyrimMod ?? throw new ArgumentNullException(nameof(skyrimMod));
    }

    public LeveledItem CreateLeveledItemFromMiscItems(string baseEditorId, List<MiscItem> miscItems, Percent chanceNone = default)
    {
        var leveledItem = CreateBaseLeveledItem(baseEditorId, chanceNone);
        AddMiscItemsToLeveledItem(leveledItem, miscItems);
        return leveledItem;
    }

    private LeveledItem CreateBaseLeveledItem(string editorId, Percent chanceNone)
    {
        var leveledItem = _skyrimMod.LeveledItems.AddNew();
        leveledItem.EditorID = editorId.AddModNamePrefix();
        leveledItem.ChanceNone = chanceNone;
        leveledItem.Entries ??= [];

        ModificationTracker.IncrementLeveledItemCount($"{leveledItem.EditorID}\tChanceNone: {chanceNone}");
        return leveledItem;
    }

    private void AddMiscItemsToLeveledItem(LeveledItem leveledItem, List<MiscItem> miscItems)
    {
        // Split items into chunks of MaxEntriesPerLeveledList
        var itemChunks = miscItems.Chunk(MaxEntriesPerLeveledList).ToList();

        if (itemChunks.Count == 0) return;

        // Create sub-lists for chunks larger than 100 items
        for (var i = 0; i < itemChunks.Count; i++)
        {
            var subList = CreateSubLeveledItem(leveledItem.EditorID ?? "Unknown", i);

            // Add MiscItems to sub-list
            foreach (var miscItem in itemChunks[i])
            {
                subList.AddToLeveledItem(miscItem);
            }

            // Add sub-list to main leveled item
            AddSubListToParent(leveledItem, subList);
        }
    }

    private LeveledItem CreateSubLeveledItem(string parentEditorId, int index)
    {
        var subListId = $"{parentEditorId}_Sub{index}";
        var subList = _skyrimMod.LeveledItems.AddNew();
        subList.EditorID = subListId;
        subList.ChanceNone = Percent.Zero;
        subList.Entries ??= [];

        ModificationTracker.IncrementLeveledItemCount($"Sub{index}\t{subList.EditorID}");
        return subList;
    }

    private static void AddSubListToParent(LeveledItem parentList, LeveledItem subList)
    {
        var entry = new LeveledItemEntry
        {
            Data = new LeveledItemEntryData
            {
                Reference = subList.ToLink(),
                Count = 1,
                Level = 1
            }
        };

        parentList.Entries ??= [];
        parentList.Entries.Add(entry);
        ModificationTracker.IncrementLeveledItemEntryCount(parentList.EditorID ?? "UNKNOWN");
    }
}