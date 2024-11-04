using CardmastersOfTamriel.SynthesisPatcher.Models;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using CardmastersOfTamriel.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Services;

public class LootDistributionService : ILootDistributionService
{
    private readonly ISkyrimMod _customMod;
    private readonly IMiscItemService _miscItemService;
    private readonly ICollection<ILootDistributorService> _distributors;

    public LootDistributionService(ISkyrimMod customMod,
        ICollection<ILootDistributorService> distributors, IMiscItemService miscItemService)
    {
        _customMod = customMod;
        _miscItemService = miscItemService;
        _distributors = distributors;
    }

    public void DistributeToCollector(ICollector collector, MasterMetadataHandler handler)
    {
        var processor = new LeveledItemProcessor(_customMod, _miscItemService);

        var collectorLeveledItem = CreateLeveledItemForCollectorType(collector.Type);

        Log.Verbose($"Creating LeveledItem: {collectorLeveledItem.EditorID} for Collector: {collector.Type}.");

        foreach (var probability in collector.CardTierProbabilities)
        {
            var cardTierLeveledItem = processor.CreateLeveledItemFromMetadata(handler, probability.Tier);
            cardTierLeveledItem.ChanceNone = probability.ChanceNone;

            collectorLeveledItem.Entries ??= [];
            AddEntriesToLeveledItem(collectorLeveledItem, cardTierLeveledItem, probability.NumberOfTimes);
        }

        collectorLeveledItem.ChanceNone = collector.ChanceNone;

        foreach (var distributor in _distributors)
        {
            distributor.DistributeLeveledItem(collector, collectorLeveledItem);
        }
    }

    private static void AddEntriesToLeveledItem(LeveledItem collectorLeveledItem, LeveledItem cardTierLeveledItem, int numberOfTimes)
    {
        for (var i = 0; i < numberOfTimes; i++)
        {
            var entry = new LeveledItemEntry
            {
                Data = new LeveledItemEntryData
                {
                    Reference = cardTierLeveledItem.ToLink(),
                    Count = 1,
                    Level = 1,
                }
            };

            collectorLeveledItem.Entries ??= [];
            collectorLeveledItem.Entries.Add(entry);
        }
    }

    private LeveledItem CreateLeveledItemForCollectorType(CollectorType collectorType)
    {
        var leveledItem = _customMod.LeveledItems.AddNew();
        leveledItem.EditorID = $"CollectorType_{collectorType}_ForLeveledItem".AddModNamePrefix();
        return leveledItem;
    }
}
