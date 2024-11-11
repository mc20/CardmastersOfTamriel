using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Collectors;
using CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Models;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace CardmastersOfTamriel.SynthesisPatcher.LeveledItems;

public class CollectorLeveledItemService
{
    private readonly ISkyrimMod _skyrimMod;
    private readonly FormIdGenerator _formIdGenerator;

    public CollectorLeveledItemService(ISkyrimMod skyrimMod, FormIdGenerator formIdGenerator)
    {
        _skyrimMod = skyrimMod;
        _formIdGenerator = formIdGenerator;
    }
    
    public void DistributeCardsToCollector(
        ICollectorConfig<LeveledItemEntry> collectorConfig, 
        IDictionary<CardTier, LeveledItem> cardMappings)
    {
        var collectorLeveledItem = CreateCollectorLeveledItem(collectorConfig);
        var collection = collectorConfig.GetCollection(collectorLeveledItem);
        DistributeItems(collection, collectorConfig.CardTierProbabilities, cardMappings, AddNpcEntry);
    }

    public void DistributeCardsToCollector(
        ICollectorConfig<ContainerEntry> collectorConfig, 
        IDictionary<CardTier, LeveledItem> cardMappings)
    {
        var collectorLeveledItem = CreateCollectorLeveledItem(collectorConfig);
        var collection = collectorConfig.GetCollection(collectorLeveledItem);
        DistributeItems(collection, collectorConfig.CardTierProbabilities, cardMappings, AddContainerEntry);
    }
    
    private LeveledItem CreateCollectorLeveledItem<T>(ICollectorConfig<T> collectorConfig)
    {
        var formKeyForCollectorConfig = _formIdGenerator.GetNextFormKey(
            $"LeveledItem_COLLECTOR{collectorConfig.Type}_{collectorConfig.Name.ToUpper()}".AddModNamePrefix());
        var collectorLeveledItem = _skyrimMod.LeveledItems.AddNew(formKeyForCollectorConfig);
        collectorLeveledItem.ChanceNone = collectorConfig.ChanceNone;
        return collectorLeveledItem;
    }

    private static void AddNpcEntry(ExtendedList<LeveledItemEntry> collection, IFormLink<IItemGetter> item)
    {
        collection.Add(new LeveledItemEntry
        {
            Data = new LeveledItemEntryData
            {
                Reference = item,
                Count = 1,
                Level = 1
            }
        });
    }

    private static void AddContainerEntry(ExtendedList<ContainerEntry> collection, IFormLink<IItemGetter> item)
    {
        collection.Add(new ContainerEntry
        {
            Item = new ContainerItem
            {
                Item = item,
                Count = 1
            }
        });
    }

    private static void DistributeItems<T>(
        ExtendedList<T> collection,
        IEnumerable<TierProbability> probabilities,
        IDictionary<CardTier, LeveledItem> cardMappings,
        Action<ExtendedList<T>, IFormLink<IItemGetter>> addAction)
    {
        foreach (var probability in probabilities)
        {
            for (var i = 0; i < probability.NumberOfTimes; i++)
            {
                addAction(collection, cardMappings[probability.Tier].ToLink());
            }
        }
    }
}