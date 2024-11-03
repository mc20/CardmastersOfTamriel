using CardmastersOfTamriel.Models;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.SynthesisPatcher.Services;

public class LeveledItemProcessor
{
    private readonly ISkyrimMod _customMod;
    private readonly IMiscItemService _service;

    public LeveledItemProcessor(ISkyrimMod customMod, IMiscItemService service)
    {
        _customMod = customMod ?? throw new ArgumentNullException(nameof(customMod));
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public LeveledItem CreateLeveledItemFromMetadata(MasterMetadataHandler masterMetadata, CardTier tier)
    {
        if (masterMetadata == null) throw new ArgumentNullException(nameof(masterMetadata));

        var tierSeriesCount = GetTierSeries(masterMetadata, tier).Count();
        var tierSetsCount = GetTierSeries(masterMetadata, tier).SelectMany(series => series.Sets ?? Enumerable.Empty<CardSet>()).Count();
        var cardSeries = GetTierSeries(masterMetadata, tier);
        LogTierSeriesInfo(tier, cardSeries);

        var tierCards = GetTierCards(cardSeries);
        var tierMiscItems = tierCards.Select(_service.InsertAsMiscItem).ToList();

        var leveledItem = CreateLeveledItemHavingEditorId($"LeveledItem_{tier}".AddModNamePrefix());

        AddMiscItemsToLeveledItem(tierMiscItems, leveledItem);

        return leveledItem;
    }

    private IEnumerable<CardSeries> GetTierSeries(MasterMetadataHandler masterMetadata, CardTier tier)
    {
        return masterMetadata.Series?.Where(series => series.Tier == tier) ?? Enumerable.Empty<CardSeries>();
    }

    private void LogTierSeriesInfo(CardTier tier, IEnumerable<CardSeries> tierSeries)
    {
        Logger.LogAction($"There are {tierSeries.Count()} series at {tier}", LogMessageType.Verbose);
        Logger.LogAction($"Tier {tier} Series has {tierSeries.SelectMany(series => series.Sets ?? Enumerable.Empty<CardSet>()).Count()} Sets", LogMessageType.Verbose);
    }

    private static List<Card> GetTierCards(IEnumerable<CardSeries> tierSeries)
    {
        return tierSeries
            .SelectMany(series => series.Sets ?? Enumerable.Empty<CardSet>())
            .SelectMany(set => set.Cards ?? Enumerable.Empty<Card>())
            .ToList();
    }

    private void AddMiscItemsToLeveledItem(List<MiscItem> miscItems, LeveledItem leveledItem)
    {
        Logger.LogAction($"Adding MiscItems to LeveledItem: {leveledItem.EditorID}");

        var miscItemSublists = CreateSublists(miscItems, 100);

        Logger.LogAction($"{miscItemSublists.Count} sublists were created from {miscItems.Count} cards");

        for (int j = 0; j < miscItemSublists.Count; j++)
        {
            var sublistLeveledItem = CreateLeveledItemHavingEditorId($"{leveledItem.EditorID}_Sublist_{j}");

            Logger.LogAction($"Adding LeveledItem sublists to {sublistLeveledItem.EditorID}");

            AddMiscItemsToSublist(miscItemSublists[j], sublistLeveledItem);

            AddLeveledItemToParentLeveledItem(sublistLeveledItem, leveledItem, 1, Percent.Zero);
        }
    }

    private List<List<MiscItem>> CreateSublists(List<MiscItem> miscItems, int sublistSize)
    {
        return miscItems
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / sublistSize)
            .Select(g => g.Select(x => x.item).ToList())
            .ToList();
    }

    private void AddMiscItemsToSublist(List<MiscItem> miscItems, LeveledItem sublistLeveledItem)
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
        return leveledList;
    }

    private void AddLeveledItemToParentLeveledItem(LeveledItem leveledItem, LeveledItem parentLeveledItem, int times, Percent chanceNone)
    {
        if (leveledItem is null || parentLeveledItem is null) return;
        if (times <= 0) times = 1;

        var modifiedParentLeveledItem = _customMod.LeveledItems.GetOrAddAsOverride(parentLeveledItem);

        // Initialize the Entries list if it's null
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

            modifiedParentLeveledItem.Entries.Add(entry);
        }

        modifiedParentLeveledItem.ChanceNone = chanceNone;
    }
}
