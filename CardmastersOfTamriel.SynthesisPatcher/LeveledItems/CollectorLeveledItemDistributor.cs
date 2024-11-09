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

    public CollectorLeveledItemDistributor(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod skyrimMod, CollectorConfigFactory configFactory, IDistributor distributor)
    {
        _state = state;
        _skyrimMod = skyrimMod;
        _configFactory = configFactory;
        _distributor = distributor;
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

    private LeveledItem? CreateLeveledItemForCollector(ICollector collector)
    {
        var leveledItemCollectorId = $"LeveledItem_Collector{collector.Type}_{_distributor.UniqueIdentifier}".AddModNamePrefix();

        if (_state.CheckIfExists<ILeveledItemGetter>(leveledItemCollectorId) ||
            _skyrimMod.CheckIfExists<LeveledItem>(leveledItemCollectorId))
        {
            Log.Warning($"LeveledItem {leveledItemCollectorId} already exists in the load order.");
            return null;
        }

        var leveledItemForCollector = _skyrimMod.LeveledItems.AddNew();
        leveledItemForCollector.EditorID = leveledItemCollectorId;
        leveledItemForCollector.ChanceNone = collector.ChanceNone;
        leveledItemForCollector.Entries ??= [];

        ModificationTracker.IncrementLeveledItemCount(
            $"Collector{collector.Type}\t{leveledItemForCollector.EditorID}\tChanceNone: {leveledItemForCollector.ChanceNone}");
        return leveledItemForCollector;
    }

    private void AddLeveledItemForCollector(ICollector collector, ITierProbability probability,
        Dictionary<CardTier, LeveledItem> tierBasedLeveledItems, LeveledItem cardCollectorLeveledItem)
    {
        var newProbabilityId = $"LeveledItem_Collector{collector.Type}_Card{probability.Tier}_{_distributor.UniqueIdentifier}".AddModNamePrefix();

        if (_state.CheckIfExists<ILeveledItemGetter>(newProbabilityId) ||
            _skyrimMod.CheckIfExists<LeveledItem>(newProbabilityId))
        {
            Log.Error($"LeveledItem {newProbabilityId} already exists in the load order.");
            return;
        }

        var newLeveledItemForCollectorProbability = _skyrimMod.LeveledItems.AddNew();
        newLeveledItemForCollectorProbability.EditorID = newProbabilityId;
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

        ModificationTracker.IncrementLeveledItemCount(
            $"Collector{collector.Type}\tProbability Card{probability.Tier}\t{newLeveledItemForCollectorProbability.EditorID}\tChanceNone: {newLeveledItemForCollectorProbability.ChanceNone}");

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

            ModificationTracker.IncrementLeveledItemEntryCount(newLeveledItemForCollectorProbability.EditorID ?? "UNKNOWN");
            cardCollectorLeveledItem.Entries ??= [];
            cardCollectorLeveledItem.Entries.Add(entry);
        }
    }
}