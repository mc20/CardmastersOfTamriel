using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Collectors.Targets;
using CardmastersOfTamriel.SynthesisPatcher.Models;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.LeveledItems;

public class CollectorLeveledItemService
{
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _skyrimMod;
    private readonly FormIdGenerator _formIdGenerator;

    public CollectorLeveledItemService(ISkyrimMod skyrimMod, FormIdGenerator formIdGenerator,
        IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        _skyrimMod = skyrimMod;
        _formIdGenerator = formIdGenerator;
        _state = state;
    }

    public void DistributeCardsToCollector(ICollectorConfig collectorConfig,
        IDictionary<CardTier, LeveledItem> cardMappings, HashSet<FormKey> collectorFormKeys, ICollectorTarget target)
    {
        // var formKeyForCollectorType = _formIdGenerator.GetNextFormKey(
        //     $"LeveledItem_COLLECTOR{collectorConfig.Type}_{collectorConfig.Name.ToUpper()}".AddModNamePrefix());
        //
        // if (_state.CheckIfExists<ILeveledItemGetter>(formKeyForCollectorType) ||
        //     _skyrimMod.CheckIfExists<LeveledItem>(formKeyForCollectorType))
        // {
        //     Log.Error($"LeveledItem with FormKey {formKeyForCollectorType} already exists.");
        //     return;
        // }
        //
        // var collectorTypeLeveledItem = _skyrimMod.LeveledItems.AddNew(formKeyForCollectorType);
        // collectorTypeLeveledItem.ChanceNone = collectorConfig.ChanceNone;
        //
        // var probabilityLeveledItem = _skyrimMod.LeveledItems.AddNew(collectorLeveledItem);
        // probabilityLeveledItem.ChanceNone = collectorConfig.ChanceNone;
        // probabilityLeveledItem.Entries ??= [];
        //
        // foreach (var probability in collectorConfig.CardTierProbabilities)
        // {
        //     target.AddItem(cardMappings[probability.Tier].ToLink());
        // }
    }
}