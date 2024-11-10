using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Configuration;
using CardmastersOfTamriel.SynthesisPatcher.Diagnostics;
using CardmastersOfTamriel.SynthesisPatcher.Models;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.LeveledItems;

public class CollectorLeveledItemDistributor
{
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _skyrimMod;
    private readonly CollectorConfigFactory _configFactory;
    private readonly IDistributor _distributor;
    private readonly FormIdGenerator _formIdGenerator;

    public CollectorLeveledItemDistributor(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod skyrimMod,
        CollectorConfigFactory configFactory, IDistributor distributor, FormIdGenerator formIdGenerator)
    {
        _state = state;
        _skyrimMod = skyrimMod;
        _configFactory = configFactory;
        _distributor = distributor;
        _formIdGenerator = formIdGenerator;
    }

    public void SetupCollectorLeveledEntries(Dictionary<CardTier, LeveledItem> cardTierItemMappings)
    {
        var collectors = Enum.GetValues(typeof(CollectorType))
            .Cast<CollectorType>()
            .Select(_configFactory.CreateCollector)
            .Where(collector => collector is not null)
            .ToList();

        foreach (var collector in collectors)
        {
            var leveledItemForCollector = CreateLeveledItemForCollector(collector!);
            if (leveledItemForCollector == null) continue;

            foreach (var probability in collector!.CardTierProbabilities)
            {
                AddLeveledItemForCollector(collector, probability, cardTierItemMappings, leveledItemForCollector);
            }

            _distributor.Distribute(collector, leveledItemForCollector);
        }
    }

    private LeveledItem? CreateLeveledItemForCollector(ICollectorConfig collectorConfig)
    {
        var leveledItemFormKey =
            _formIdGenerator.GetNextFormKey($"LeveledItem_COLLECTOR{collectorConfig.Type}_{_distributor.UniqueIdentifier}"
                .AddModNamePrefix());

        if (_state.CheckIfExists<ILeveledItemGetter>(leveledItemFormKey) ||
            _skyrimMod.CheckIfExists<LeveledItem>(leveledItemFormKey))
        {
            Log.Warning($"LeveledItem {leveledItemFormKey} already exists in the load order.");
            return null;
        }

        var leveledItemForCollector = _skyrimMod.LeveledItems.AddNew(leveledItemFormKey);
        leveledItemForCollector.ChanceNone = collectorConfig.ChanceNone;
        leveledItemForCollector.Entries ??= [];

        ModificationTracker.IncrementLeveledItemCount($"Collector{collectorConfig.Type}" +
                                                      $"\t{leveledItemForCollector.FormKey}" +
                                                      $"\tChanceNone: {leveledItemForCollector.ChanceNone}");
        return leveledItemForCollector;
    }

    private void AddLeveledItemForCollector(ICollectorConfig collectorConfig, ITierProbability probability,
        Dictionary<CardTier, LeveledItem> tierBasedLeveledItems, LeveledItem cardCollectorLeveledItem)
    {
        var newProbabilityFormKey = _formIdGenerator.GetNextFormKey(
            $"LeveledItem_COLLECTOR{collectorConfig.Type}_CARD{probability.Tier}_{_distributor.UniqueIdentifier}"
                .AddModNamePrefix());

        if (_state.CheckIfExists<ILeveledItemGetter>(newProbabilityFormKey) ||
            _skyrimMod.CheckIfExists<LeveledItem>(newProbabilityFormKey))
        {
            Log.Error($"LeveledItem {newProbabilityFormKey} already exists in the load order.");
            return;
        }

        var newLeveledItemForCollectorProbability = _skyrimMod.LeveledItems.AddNew(newProbabilityFormKey);
        newLeveledItemForCollectorProbability.ChanceNone = probability.ChanceNone;
        newLeveledItemForCollectorProbability.Entries ??= [];

        var tierEntry = new LeveledItemEntry
        {
            Data = new LeveledItemEntryData
            {
                Reference = tierBasedLeveledItems[probability.Tier].ToLink(),
                Count = 1,
                Level = 1
            }
        };

        newLeveledItemForCollectorProbability.Entries.Add(tierEntry);

        ModificationTracker.IncrementLeveledItemCount($"Collector{collectorConfig.Type}" +
                                                      $"\tProbability Card{probability.Tier}" +
                                                      $"\t{newLeveledItemForCollectorProbability.FormKey}" +
                                                      $"\tChanceNone: {newLeveledItemForCollectorProbability.ChanceNone}");

        for (var i = 0; i < probability.NumberOfTimes; i++)
        {
            var entry = new LeveledItemEntry
            {
                Data = new LeveledItemEntryData
                {
                    Reference = newLeveledItemForCollectorProbability.ToLink(),
                    Count = 1,
                    Level = 1
                }
            };

            ModificationTracker.IncrementLeveledItemEntryCount(
                newLeveledItemForCollectorProbability.FormKey.ToString() ??
                "UNKNOWN");
            cardCollectorLeveledItem.Entries ??= [];
            cardCollectorLeveledItem.Entries.Add(entry);
        }
    }
}