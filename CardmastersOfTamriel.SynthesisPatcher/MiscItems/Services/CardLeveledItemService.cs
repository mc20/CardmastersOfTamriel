using CardmastersOfTamriel.Models;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using CardmastersOfTamriel.Utilities;
using Serilog;
using CardmastersOfTamriel.SynthesisPatcher.LeveledItems;
using CardmastersOfTamriel.SynthesisPatcher.Metadata;

namespace CardmastersOfTamriel.SynthesisPatcher.MiscItems.Services;

public class CardLeveledItemService
{
    private readonly MasterMetadataHandler _metadataHandler;
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _customMod;

    public CardLeveledItemService(MasterMetadataHandler metadataHandler, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod)
    {
        _metadataHandler = metadataHandler;
        _state = state;
        _customMod = customMod;
    }

    public Dictionary<CardTier, LeveledItem> CreateCardTierToLeveledItemMapping()
    {
        Log.Information("Creating CardTier to LeveledItem mapping..");

        var helper = new MetadataHelper(_metadataHandler);
        var cardList = helper.GetCards().ToHashSet();

        var miscService = new CardToMiscItemService(_state, _customMod);
        var mappedMiscItems = miscService.InsertAndMapCardsToMiscItems(cardList);

        // Get all cards grouped by CardTier
        var cardTierItemCreator = new TieredCardLeveledItemAssembler(_state, _customMod);
        return cardTierItemCreator.CreateCardTierLeveledItems(mappedMiscItems);
    }
}