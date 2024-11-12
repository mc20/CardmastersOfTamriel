using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Models;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.LeveledItems;

public class CollectorProbabilityMapper
{
    private readonly ISkyrimMod _skyrimMod;
    private readonly Dictionary<CardTier, LeveledItem> _cardTierToLeveledItemMapping;

    public CollectorProbabilityMapper(ISkyrimMod skyrimMod,
        Dictionary<CardTier, LeveledItem> cardTierToLeveledItemMapping)
    {
        _skyrimMod = skyrimMod;
        _cardTierToLeveledItemMapping = cardTierToLeveledItemMapping;
    }

    public Dictionary<CollectorType, LeveledItem> CreateCollectorTypeMapping(CollectorTypeConfiguration config)
    {
        Log.Information("Creating collector type mapping..");

        var items = new Dictionary<CollectorType, LeveledItem>();
        foreach (var collectorType in config.CollectorTypes)
        {
            var collectorId = $"CATEGORY_{config.Category}_Collector{collectorType.Type}".AddModNamePrefix();

            Log.Verbose($"Mapping new LeveledItem to {collectorType}: '{collectorId}'");

            var collectorLeveledItem = _skyrimMod.LeveledItems.AddNewWithId(collectorId);
            collectorLeveledItem.ChanceNone = new Percent(collectorType.ChanceNone);

            foreach (var probability in collectorType.CardTierProbabilities)
            {
                var probId = collectorId + "_Tier" + probability.Tier;
                var probabilityLeveledItem = _skyrimMod.LeveledItems.AddNewWithId(probId);
                probabilityLeveledItem.ChanceNone = new Percent(probability.ChanceNone);

                _cardTierToLeveledItemMapping.TryGetValue(probability.Tier, out var cardTierLeveledItem);

                if (cardTierLeveledItem == null)
                {
                    Log.Error($"LeveledItem for CardTier {probability.Tier} not found.");
                    continue;
                }

                LeveledItemEntryBuilder.AddEntries(
                    probabilityLeveledItem,
                    cardTierLeveledItem,
                    count: 1,
                    numberOfTimes: probability.NumberOfTimes);

                LeveledItemEntryBuilder.AddEntries(
                    collectorLeveledItem,
                    probabilityLeveledItem,
                    count: 1,
                    numberOfTimes: 1);
            }

            items.Add(collectorType.Type, collectorLeveledItem);
        }

        return items;
    }
}