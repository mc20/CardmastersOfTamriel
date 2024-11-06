using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Diagnostics;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.LeveledItems;

public class TieredCardLeveledItemAssembler
{
    private const int MAX_ENTRIES_PER_LEVELED_LIST = 100;
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _skyrimMod;

    public TieredCardLeveledItemAssembler(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod skyrimMod)
    {
        _state = state;
        _skyrimMod = skyrimMod;
    }

    public Dictionary<CardTier, LeveledItem> CreateCardTierLeveledItems(Dictionary<Card, MiscItem> miscItems)
    {
        var cardTierLeveledItems = new Dictionary<CardTier, LeveledItem>();

        foreach (var tier in Enum.GetValues(typeof(CardTier)).Cast<CardTier>())
        {
            var tierLeveledItem = CreateTierLeveledItem(tier);
            if (tierLeveledItem == null) continue;

            var tierMiscItems = GetMiscItemsForTier(miscItems, tier);
            AddMiscItemsToTierLeveledItem(tierLeveledItem, tierMiscItems, tier);

            cardTierLeveledItems.Add(tier, tierLeveledItem);
        }

        return cardTierLeveledItems;
    }

    private LeveledItem? CreateTierLeveledItem(CardTier tier)
    {
        var leveledItemId = $"LeveledItem_CardTier{tier}".AddModNamePrefix();

        if (_state.CheckIfExists<ILeveledItemGetter>(leveledItemId) ||
            _skyrimMod.CheckIfExists<LeveledItem>(leveledItemId))
        {
            Log.Warning($"LeveledItem {leveledItemId} already exists in the load order.");
            return null;
        }

        var leveledItem = _skyrimMod.LeveledItems.AddNew();
        leveledItem.EditorID = leveledItemId;
        leveledItem.ChanceNone = Percent.Zero;
        leveledItem.Entries ??= [];

        ModificationTracker.IncrementLeveledItemCount($"Card{tier}\t{leveledItem.EditorID}");
        return leveledItem;
    }

    private static List<MiscItem> GetMiscItemsForTier(Dictionary<Card, MiscItem> miscItems, CardTier tier)
    {
        return miscItems
            .Where(item => item.Key.Tier == tier)
            .Select(item => item.Value)
            .ToList();
    }

    private void AddMiscItemsToTierLeveledItem(LeveledItem tierLeveledItem, List<MiscItem> tierMiscItems, CardTier tier)
    {
        // Split items into chunks of MAX_ENTRIES_PER_LEVELED_LIST
        var itemChunks = tierMiscItems.Chunk(MAX_ENTRIES_PER_LEVELED_LIST).ToList();

        if (itemChunks.Count == 0) return;

        // If we have 100 or fewer items, add them directly to the tier leveled item
        if (itemChunks.Count == 1)
        {
            foreach (var miscItem in itemChunks[0])
            {
                AddMiscItemToLeveledItem(tierLeveledItem, miscItem);
            }
            return;
        }

        // Create sub-lists for chunks larger than 100 items
        for (int i = 0; i < itemChunks.Count; i++)
        {
            var subList = CreateSubLeveledItem(tier, i);
            if (subList == null) continue;

            // Add MiscItems to sub-list
            foreach (var miscItem in itemChunks[i])
            {
                AddMiscItemToLeveledItem(subList, miscItem);
            }

            // Add sub-list to tier leveled item
            AddSubListToTierLeveledItem(tierLeveledItem, subList);
        }
    }

    private LeveledItem? CreateSubLeveledItem(CardTier tier, int index)
    {
        var subListId = $"LeveledItem_CardTier{tier}_Sub{index}".AddModNamePrefix();

        if (_state.CheckIfExists<ILeveledItemGetter>(subListId) ||
            _skyrimMod.CheckIfExists<LeveledItem>(subListId))
        {
            Log.Warning($"LeveledItem {subListId} already exists in the load order.");
            return null;
        }

        var subList = _skyrimMod.LeveledItems.AddNew();
        subList.EditorID = subListId;
        subList.ChanceNone = Percent.Zero;
        subList.Entries ??= [];

        ModificationTracker.IncrementLeveledItemCount($"Card{tier}_Sub{index}\t{subList.EditorID}");
        return subList;
    }

    private static void AddMiscItemToLeveledItem(LeveledItem leveledItem, MiscItem miscItem)
    {
        var entry = new LeveledItemEntry
        {
            Data = new LeveledItemEntryData
            {
                Reference = miscItem.ToLink(),
                Count = 1,
                Level = 1
            }
        };
        leveledItem.Entries ??= [];
        leveledItem.Entries.Add(entry);
        ModificationTracker.IncrementLeveledItemEntryCount(leveledItem.EditorID ?? "UNKNOWN");
    }

    private static void AddSubListToTierLeveledItem(LeveledItem tierLeveledItem, LeveledItem subList)
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
        tierLeveledItem.Entries ??= [];
        tierLeveledItem.Entries.Add(entry);
        ModificationTracker.IncrementLeveledItemEntryCount(tierLeveledItem.EditorID ?? "UNKNOWN");
    }
}