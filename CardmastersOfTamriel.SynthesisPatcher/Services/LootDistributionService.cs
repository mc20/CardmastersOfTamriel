using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using CardmastersOfTamriel.SynthesisPatcher.Services;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Models;

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

        var editorIdForLeveledItem = $"Collector_{collector.Type}_ForLeveledItem".AddModNamePrefix();
        var collectorLeveledItem = CreateLeveledItemHavingEditorId(editorIdForLeveledItem);

        foreach (var probability in collector.CardTierProbabilities)
        {
            var cardTierLeveledItem = processor.ProcessTier(_metadata, probability.Tier);
            cardTierLeveledItem.ChanceNone = probability.ChanceNone;

            collectorLeveledItem.Entries ??= [];
            for (var i = 0; i < probability.NumberOfTimes; i++)
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

                collectorLeveledItem.Entries.Add(entry);
            }

            collectorLeveledItem.ChanceNone = collector.ChanceNone;
        }

        foreach (var distributor in _distributors)
        {
            distributor.DistributeLeveledItem(collectorLeveledItem);
        }
    }

    private LeveledItem CreateLeveledItemHavingEditorId(string editorId)
    {
        var leveledList = _customMod.LeveledItems.AddNew();
        leveledList.EditorID = editorId;
        return leveledList;
    }
}
