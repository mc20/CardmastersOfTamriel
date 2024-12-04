using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace CardmastersOfTamriel.SynthesisPatcher.LeveledItems;

public class TieredCardLeveledItemAssembler
{
    private const int MaxEntriesPerLeveledList = 100;
    private readonly ISkyrimMod _skyrimMod;

    public TieredCardLeveledItemAssembler(ISkyrimMod skyrimMod)
    {
        _skyrimMod = skyrimMod;
    }

    /// <summary>
    /// Creates a dictionary of leveled items for each card tier.
    /// </summary>
    /// <param name="miscItems">A dictionary of cards and their corresponding miscellaneous items.</param>
    /// <returns>A dictionary where the key is the card tier and the value is the leveled item for that tier.</returns>
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
        var leveledItem = _skyrimMod.LeveledItems.AddNewWithId($"LeveledItem_Card{tier}".AddModNamePrefix());
        leveledItem.ChanceNone = Percent.Zero;
        leveledItem.Entries ??= [];

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
        // Split items into chunks of MaxEntriesPerLeveledList
        var itemChunks = tierMiscItems.Chunk(MaxEntriesPerLeveledList).ToList();

        if (itemChunks.Count == 0) return;

        // Create sub-lists for all chunks
        for (var i = 0; i < itemChunks.Count; i++)
        {
            var subList = CreateSubLeveledItem(tier, i);
            if (subList == null) continue;

            // Add MiscItems to sub-list
            foreach (var miscItem in itemChunks[i]) AddMiscItemToLeveledItem(subList, miscItem);

            // Add sub-list to tier leveled item
            AddSubListToTierLeveledItem(tierLeveledItem, subList);
        }
    }

    private LeveledItem? CreateSubLeveledItem(CardTier tier, int index)
    {
        var subList = _skyrimMod.LeveledItems.AddNewWithId($"LeveledItem_Card{tier}_SubList{index:D4}".AddModNamePrefix());
        subList.ChanceNone = Percent.Zero;
        subList.Entries ??= [];

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
    }
}