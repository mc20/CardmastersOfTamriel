using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using CardmastersOfTamriel.Utilities;
using Serilog;

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

    public LeveledItem CreateLeveledItemFromMetadata(MasterMetadataHandler handler, CardTier tier)
    {
        // var tierSeriesCount = GetTierSeries(tier).Count();
        // var tierSetsCount = GetTierSeries(tier).SelectMany(series => series.Sets ?? Enumerable.Empty<CardSet>()).Count();
        var cardSeries = handler.Metadata.Series?.Where(series => series.Tier == tier).ToList() ?? [];
        LogTierSeriesInfo(tier, cardSeries);

        var tierCards = GetTierCards(cardSeries);
        var tierMiscItems = tierCards.Select(_service.InsertAsMiscItem).ToList();

        var leveledItem = CreateLeveledItemHavingEditorId($"LeveledItem_{tier}".AddModNamePrefix());

        AddMiscItemsToLeveledItem(tierMiscItems, leveledItem);

        return leveledItem;
    }

    private void LogTierSeriesInfo(CardTier tier, List<CardSeries> tierSeries)
    {
        Log.Verbose($"There are {tierSeries.Count} series at {tier}");
        Log.Verbose(
            $"Tier {tier} Series has {tierSeries.SelectMany(series => series.Sets ?? Enumerable.Empty<CardSet>()).Count()} Sets");
    }

    private static List<Card> GetTierCards(IEnumerable<CardSeries> tierSeries)
    {
        return tierSeries
            .SelectMany(series => series.Sets ?? Enumerable.Empty<CardSet>())
            .SelectMany(set => set.Cards ?? Enumerable.Empty<Card>())
            .Where(card => !string.IsNullOrWhiteSpace(card.DestinationFilePath))
            .ToList();
    }

    private void AddMiscItemsToLeveledItem(List<MiscItem> miscItems, LeveledItem leveledItem)
    {
        Log.Information($"Adding MiscItems to LeveledItem: {leveledItem.EditorID}");

        var miscItemSublists = CreateSublists(miscItems, 100);

        Log.Information($"{miscItemSublists.Count} sublists were created from {miscItems.Count} cards");

        for (int j = 0; j < miscItemSublists.Count; j++)
        {
            var sublistLeveledItem = CreateLeveledItemHavingEditorId($"{leveledItem.EditorID}_Sublist_{j}");

            Log.Information($"Adding LeveledItem sublists to {sublistLeveledItem.EditorID}");

            AddMiscItemsToSublist(miscItemSublists[j], sublistLeveledItem);

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

    private void AddLeveledItemToParentLeveledItem(LeveledItem leveledItem, LeveledItem parentLeveledItem, int times,
        Percent chanceNone)
    {
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