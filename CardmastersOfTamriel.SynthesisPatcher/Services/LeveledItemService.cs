using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Models;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace CardmastersOfTamriel.SynthesisPatcher.Services;

public class LeveledItemService
{
    // private readonly LeveledItemProcessor _processor;
    // private readonly LeveledItemDistributionService _distributor;

    // public LeveledItemService(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod)
    // {
    //     var service = new MiscItemService(state, customMod);
    //     _processor = new LeveledItemProcessor(customMod, service);
    //     _distributor = new LeveledItemDistributionService(state, customMod);
    // }

    // public void ProcessAndDistributeLeveledItems(MasterMetadata masterMetadata)
    // {
    //     var collections = new CardCollections
    //     {
    //         Tier1LeveledItem = _processor.ProcessTier(masterMetadata, CardTier.Tier1),
    //         Tier2LeveledItem = _processor.ProcessTier(masterMetadata, CardTier.Tier2),
    //         Tier3LeveledItem = _processor.ProcessTier(masterMetadata, CardTier.Tier3),
    //         Tier4LeveledItem = _processor.ProcessTier(masterMetadata, CardTier.Tier4),
    //     };

    //     foreach (var collectorType in Enum.GetValues<CollectorType>())
    //     {
    //         _distributor.DistributeLeveledItems(collectorType, collections);
    //     }
    // }
}