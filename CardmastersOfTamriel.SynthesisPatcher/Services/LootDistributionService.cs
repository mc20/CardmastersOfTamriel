using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using CardmastersOfTamriel.SynthesisPatcher.Services;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Models;
using CardmastersOfTamriel.Utilities;
using Noggog;

namespace CardmastersOfTamriel.SynthesisPatcher;

public class LootDistributionService : ILootDistributionService
{
    private readonly ISkyrimMod _customMod;
    private readonly IMiscItemService _miscItemService;
    private readonly MasterMetadata _metadata;
    private readonly ICollection<ILootDistributorService> _distributors;

    public LootDistributionService(ISkyrimMod customMod, ICollection<ILootDistributorService> distributors, IMiscItemService miscItemService, MasterMetadata metadata)
    {
        _customMod = customMod;
        _miscItemService = miscItemService;
        _metadata = metadata;
        _distributors = distributors;
    }

    public void DistributeToCollector(ICollector collector)
    {
        var processor = new LeveledItemProcessor(_customMod, _miscItemService);

        var collectorLeveledItem = CreateLeveledItemForCollectorType(collector.Type);

        DebugTools.LogAction($"Creating LeveledItem: {collectorLeveledItem.EditorID} for Collector: {collector.Type}.", LogMessageType.VERBOSE);

        foreach (var probability in collector.CardTierProbabilities)
        {
            var cardTierLeveledItem = processor.CreateLeveledItemFromMetadata(_metadata, probability.Tier);
            cardTierLeveledItem.ChanceNone = probability.ChanceNone;

            collectorLeveledItem.Entries ??= new ExtendedList<LeveledItemEntry>();
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

            collectorLeveledItem.Entries ??= new ExtendedList<LeveledItemEntry>();
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
