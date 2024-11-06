using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.SynthesisPatcher.Services;

public class LeveledItemProcessor
{
    private readonly ISkyrimMod _customMod;
    private readonly MasterMetadataHandler _handler;

    public LeveledItemProcessor(ISkyrimMod customMod, MasterMetadataHandler handler)
    {
        _customMod = customMod ?? throw new ArgumentNullException(nameof(customMod));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public LeveledItem CreateLeveledItemFromMetadata(CardTier tier, List<MiscItem> miscItems)
    {
        var leveledItem = CreateLeveledItemHavingEditorId($"LeveledItem_{tier}".AddModNamePrefix());

        AddMiscItemsToLeveledItem(leveledItem, miscItems);

        return leveledItem;
    }

    private void AddMiscItemsToLeveledItem(LeveledItem leveledItem, List<MiscItem> miscItems)
    {
        var miscItemSublists = CreateSublists(miscItems, 100);

        for (int j = 0; j < miscItemSublists.Count; j++)
        {
            var sublistLeveledItem = CreateLeveledItemHavingEditorId($"{leveledItem.EditorID}_Sublist_{j}");

            AddMiscItemsToSublist(sublistLeveledItem, miscItemSublists[j]);
            AddLeveledItemToParentLeveledItem(sublistLeveledItem, leveledItem, 1, Percent.Zero);
        }
    }

    private static List<List<MiscItem>> CreateSublists(List<MiscItem> miscItems, int sublistSize)
    {
        return miscItems
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / sublistSize)
            .Select(g => g.Select(x => x.item).ToList())
            .ToList();
    }

    private static void AddMiscItemsToSublist(LeveledItem sublistLeveledItem, List<MiscItem> miscItems)
    {
        foreach (var miscItem in miscItems.Where(c => c?.EditorID is not null))
        {
            sublistLeveledItem.AddToLeveledItem(miscItem);
        }
    }

    private LeveledItem CreateLeveledItemHavingEditorId(string editorId)
    {
        var leveledList = _customMod.LeveledItems.AddNew();
        leveledList.EditorID = editorId;
        Counters.IncrementLeveledItemCount(leveledList.EditorID);
        return leveledList;
    }

    private void AddLeveledItemToParentLeveledItem(LeveledItem leveledItem, LeveledItem parentLeveledItem, int times,
        Percent chanceNone)
    {
        if (times <= 0) times = 1;

        var modifiedParentLeveledItem = _customMod.LeveledItems.GetOrAddAsOverride(parentLeveledItem);
        modifiedParentLeveledItem.Entries ??= [];
        for (var i = 0; i < times; i++)
        {
            var entry = new LeveledItemEntry
            {
                Data = new LeveledItemEntryData
                {
                    Reference = leveledItem.ToLink(),
                    Count = 1,
                    Level = 1
                }
            };

            Counters.IncrementLeveledItemEntryCount(modifiedParentLeveledItem.EditorID ?? "UNKNOWN");
            modifiedParentLeveledItem.Entries.Add(entry);
        }

        modifiedParentLeveledItem.ChanceNone = chanceNone;
    }
}