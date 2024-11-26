using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Common.Configuration;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using CardmastersOfTamriel.Utilities;
using Serilog;
using CardmastersOfTamriel.SynthesisPatcher.LeveledItems;

namespace CardmastersOfTamriel.SynthesisPatcher.MiscItems.Services;

public class CardLeveledItemService
{
    private readonly PatcherConfiguration _patcherConfig;
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _customMod;
    private readonly Dictionary<string, string> _keywordsBySeries;

    public CardLeveledItemService(PatcherConfiguration patcherConfig, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod, Dictionary<string, string> keywordsBySeries)
    {
        _patcherConfig = patcherConfig;
        _state = state;
        _customMod = customMod;
        _keywordsBySeries = keywordsBySeries;
    }

    public async Task<Dictionary<CardTier, LeveledItem>> CreateCardTierToLeveledItemMappingAsync(CancellationToken cancellationToken)
    {
        Log.Information("Creating CardTier to LeveledItem mapping..");

        var cardList = await GetCards(cancellationToken);

        var miscService = new CardToMiscItemService(_state, _customMod, _keywordsBySeries);
        var mappedMiscItems = miscService.InsertAndMapCardsToMiscItems(cardList);

        // Get all cards grouped by CardTier
        var cardTierItemCreator = new TieredCardLeveledItemAssembler(_customMod);
        return cardTierItemCreator.CreateCardTierLeveledItems(mappedMiscItems);
    }
    
    private async Task<HashSet<Card>> GetCards(CancellationToken cancellationToken)
    {
        Log.Information("Getting cards from metadata.");
        var metadataFilePath = _patcherConfig.MasterMetadataFilePath = _state.RetrieveInternalFile(_patcherConfig.MasterMetadataFilePath);
        var data = await JsonFileReader.ReadFromJsonAsync<Dictionary<CardTier, HashSet<CardSeries>>>(metadataFilePath, cancellationToken);
        var allSeriesData = data.Values.SelectMany(series => series).ToHashSet();
        var allSetsData = allSeriesData.SelectMany(set => set.Sets ?? []).ToHashSet();
        var allCardsData = allSetsData.SelectMany(set => set.Cards ?? []).ToHashSet();
        return allCardsData.Where(card => !string.IsNullOrWhiteSpace(card.DestinationRelativeFilePath)).ToHashSet();
    }
}