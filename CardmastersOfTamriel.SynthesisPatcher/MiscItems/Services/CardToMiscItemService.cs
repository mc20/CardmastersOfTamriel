using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.MiscItems.Factory;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.MiscItems.Services;

public class CardToMiscItemService
{
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _customMod;
    private readonly Dictionary<string, string> _keywordsBySeries;

    public CardToMiscItemService(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod,
        Dictionary<string, string> keywordsBySeries)
    {
        _state = state;
        _customMod = customMod;
        _keywordsBySeries = keywordsBySeries;
    }

    /// <summary>
    /// Inserts a collection of cards as miscellaneous items and maps each card to its corresponding miscellaneous item.
    /// </summary>
    /// <param name="cards">A collection of cards to be inserted as miscellaneous items.</param>
    /// <returns>A dictionary mapping each card to its corresponding miscellaneous item.</returns>
    public Dictionary<Card, MiscItem> InsertAndMapCardsToMiscItems(HashSet<Card> cards)
    {
        var miscItems = new Dictionary<Card, MiscItem>();

        Log.Debug($"Inserting {cards.Count} cards as MiscItems..");

        foreach (var card in cards)
        {
            var miscItem = InsertAsMiscItem(card);
            if (miscItem == null) continue;


            miscItems.Add(card, miscItem);
            Log.Debug($"Inserted MiscItem: {miscItem.EditorID}");
        }

        return miscItems;
    }

    private MiscItem? InsertAsMiscItem(Card card)
    {
        var newMiscItem = MiscItemFactory.CreateMiscItem(_customMod, card);
        var textureSetForWorldModel = TextureSetFactory.CreateTextureSet(_customMod, card);

        newMiscItem.Model = ModelFactory.CreateModel(card, textureSetForWorldModel);

        card.Keywords ??= [];

        if (_keywordsBySeries.TryGetValue(card.SetId, out var value))
        {
            Log.Debug($"Adding keyword {value} to MiscItem: {newMiscItem.EditorID}");
            card.Keywords.Add(value);
        }


        if (card.Keywords.Count != 0)
        {
            Log.Debug($"Adding {card.Keywords.Count} keywords to MiscItem: {newMiscItem.EditorID}");
            AddKeywordsToMiscItem(newMiscItem, card.Keywords);
        }

        Log.Verbose($"Card: {card.Id} '{card.DisplayName}' inserted as MiscItem: {newMiscItem.EditorID}");

        return newMiscItem;
    }

    private void AddKeywordsToMiscItem(MiscItem miscItem, HashSet<string> keywordEditorIDs)
    {
        var keywordNotFound = string.Empty;

        miscItem.Keywords ??= [];
        foreach (var keywordEditorId in keywordEditorIDs)
        {
            keywordNotFound = !keywordNotFound.Equals(keywordEditorId) ? keywordEditorId : string.Empty;

            var keyword = _state.LoadOrder.PriorityOrder.Keyword().WinningOverrides()
                .FirstOrDefault(kw => kw.EditorID == keywordEditorId);
            if (keyword is null && string.IsNullOrWhiteSpace(keywordNotFound))
            {
                keywordNotFound = keywordEditorId;
                Log.Warning($"Keyword {keywordEditorId} not found in the Game load order.");
            }

            keyword ??= _customMod.Keywords.FirstOrDefault(kw => kw.EditorID == keywordEditorId);
            if (keyword is null && string.IsNullOrWhiteSpace(keywordNotFound))
            {
                keywordNotFound = keywordEditorId;
                Log.Warning($"Keyword {keywordEditorId} not found in the Mod load order.");
            }

            if (keyword != null)
            {
                miscItem.Keywords.Add(keyword.ToLink());
            }
            else
            {
                Log.Warning($"Keyword {keywordEditorId} not found in the load order.");
            }
        }
    }
}