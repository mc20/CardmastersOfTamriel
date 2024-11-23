using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Models;
using CardmastersOfTamriel.SynthesisPatcher.Common.Configuration;
using CardmastersOfTamriel.SynthesisPatcher.Distribution.Strategies;
using CardmastersOfTamriel.SynthesisPatcher.LeveledItems;
using CardmastersOfTamriel.Utilities;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Distribution;

/// <summary>
/// Handles the distribution of card collections throughout the game world by managing different distribution strategies
/// for various game object types (LeveledItems, Containers, and NPCs).
/// </summary>
/// <remarks>
/// This distributor uses type-specific strategies to handle card distribution based on collector configurations
/// and probability mappings. It supports:
/// - Distribution to LeveledItems
/// - Distribution to Containers
/// - Distribution to NPCs
/// Each strategy is initialized with its own configuration and handles the specifics of distribution for its type.
/// </remarks>
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

    private Dictionary<Type, ICardDistributionStrategy> InitializeStrategies() => new()
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

    public async Task DistributeToCollectorsInWorldAsync<T>(CancellationToken cancellationToken)
    {
        Log.Information($"Distributing cards to {typeof(T).Name} collectors in world..");

        DebugExistingLoadOrder();

        if (!_strategies.TryGetValue(typeof(T), out var strategy))
        {
            throw new NotImplementedException(
                $"Distribution strategy for type {typeof(T).Name} is not implemented");
        }

        await DistributeCardsViaStrategyAsync(strategy, cancellationToken);
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

    private async Task DistributeCardsViaStrategyAsync(ICardDistributionStrategy strategy, CancellationToken cancellationToken)
    {
        Log.Information("Setting up entries for LeveledItems..");

        var configuration = await JsonFileReader.ReadFromJsonAsync<CollectorTypeConfiguration>(strategy.Configuration.DistributionFilePath, cancellationToken);
        Log.Information($"Loaded Configuration for: {configuration.Category} from '{strategy.Configuration.DistributionFilePath}'");

        var collectorLeveledListMapping = _probabilityLeveledListBuilder.CreateCollectorTypeMapping(configuration);
        Log.Information($"Mapped {collectorLeveledListMapping.Count} CollectorTypes with LeveledItems.");

        var mappings = await CollectorLoader.GetCollectorIdsAsync(strategy.Configuration.CollectorConfigFilePaths, cancellationToken);
        var collectorTypeMappings = mappings.OrderBy(
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