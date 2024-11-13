using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Factory;
using CardmastersOfTamriel.SynthesisPatcher.Configuration;
using CardmastersOfTamriel.SynthesisPatcher.Distribution.Strategies;
using CardmastersOfTamriel.SynthesisPatcher.LeveledItems;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Distribution;

public class CardCollectionDistributor
{
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _customMod;
    private readonly CollectorProbabilityMappingService _probabilityLeveledListBuilder;
    private readonly PatcherConfiguration _configuration;
    private readonly IDictionary<Type, ICardDistributionStrategy> _strategies;

    public CardCollectionDistributor(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod,
         CollectorProbabilityMappingService probabilityLeveledListBuilder, PatcherConfiguration config)
    {
        _state = state;
        _customMod = customMod;
        _probabilityLeveledListBuilder = probabilityLeveledListBuilder;
        _configuration = config;
        _strategies = InitializeStrategies();
    }

    private IDictionary<Type, ICardDistributionStrategy> InitializeStrategies()
    {
        return new Dictionary<Type, ICardDistributionStrategy>
        {
            {
                typeof(LeveledItem),
                new LeveledItemDistributionStrategy(
                    _state,
                    _customMod,
                    _configuration.GetConfigurationForTarget("leveleditem"))
            },
            {
                typeof(Container),
                new ContainerDistributionStrategy(
                    _state,
                    _customMod,
                    _configuration.GetConfigurationForTarget("containers"))
            },
            {
                typeof(Npc),
                new NpcDistributionStrategy(
                    _state,
                    _customMod,
                    _configuration.GetConfigurationForTarget("npcs"))
            }
        };
    }
    public void DistributeToCollectorsInWorld<T>()
    {
        // DebugExistingLoadOrder();

        if (!_strategies.TryGetValue(typeof(T), out var strategy))
        {
            throw new NotImplementedException(
                $"Distribution strategy for type {typeof(T).Name} is not implemented");
        }

        DistributeCardsViaStrategy(strategy);
    }

    private void DebugExistingLoadOrder()
    {
        Log.Debug("Loading LeveledItems..");
        _state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides().OrderBy(x => x.EditorID)
                                      .ForEach(l => Log.Debug("LoadOrder -> LeveledItem: {0}", l.EditorID));

        Log.Debug("Loading Containers..");
        _state.LoadOrder.PriorityOrder.Container().WinningOverrides().OrderBy(x => x.EditorID)
                                      .ForEach(l => Log.Debug("LoadOrder -> Container: {0}", l.EditorID));

        Log.Debug("LoadOrder -> Loading Npcs..");
        _state.LoadOrder.PriorityOrder.Npc().WinningOverrides().OrderBy(x => x.EditorID)
                                      .ForEach(l => Log.Debug("Npc: {0}", l.EditorID));
    }

    private void DistributeCardsViaStrategy(ICardDistributionStrategy strategy)
    {
        Log.Information("Setting up entries for LeveledItems..");

        var configuration = CollectorConfigFactory.RetrieveCollectorConfiguration(strategy.Configuration.DistributionFilePath);
        Log.Verbose($"Loaded Configuration for: {configuration.Category} from '{strategy.Configuration.DistributionFilePath}'");

        var collectorLeveledListMapping = _probabilityLeveledListBuilder.CreateCollectorTypeMapping(configuration);
        Log.Verbose($"Mapped {collectorLeveledListMapping.Count} CollectorTypes with LeveledItems.");

        var collectorTypeMappings = CollectorLoader.GetCollectorIds(strategy.Configuration.CollectorConfigFilePath).OrderBy(
            kvp => new[]
                {
                    CollectorType.Tier1, CollectorType.Tier2, CollectorType.Tier3, CollectorType.Tier4,
                    CollectorType.Tier5, CollectorType.Tier6, CollectorType.MasterTier
                }.ToList()
                .IndexOf(kvp.Key)).ToDictionary(
            kvp => kvp.Key,
            // Sort the HashSet values alphabetically
            kvp => new SortedSet<string>(kvp.Value, StringComparer.Ordinal)
        );

        foreach (var collectorTier in collectorTypeMappings.OrderBy(ct => ct.Key.ToString()))
        {
            Log.Verbose("Distributing cards for CollectorType '{0}'..", collectorTier.Key);

            foreach (var targetEditorId in collectorTier.Value)
            {
                if (!collectorLeveledListMapping.TryGetValue(collectorTier.Key, out var cardTierLeveledItem)) continue;

                Log.Verbose($"Adding LeveledItem '{cardTierLeveledItem.EditorID}' as Entry for LeveledItem '{targetEditorId}'..");
                strategy.DistributeToTarget(cardTierLeveledItem, targetEditorId);
            }
        }
    }
}