using CardmastersOfTamriel.SynthesisPatcher.MiscItems.Factory;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.MiscItems.Services;

public class CardToMiscItemService
{
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _customMod;


    public CardToMiscItemService(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod)
    {
        _state = state;
        _customMod = customMod;
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
            Log.Verbose($"Inserted MiscItem: {miscItem.EditorID}");
        }

        return miscItems;
    }

    private MiscItem? InsertAsMiscItem(Card card)
    {
        var newMiscItem = MiscItemFactory.CreateMiscItem(_customMod, card);
        var textureSetForWorldModel = TextureSetFactory.CreateTextureSet(_customMod, card);

        newMiscItem.Model = ModelFactory.CreateModel(card, textureSetForWorldModel);

        card.Keywords ??= [];

        var kw = card.Tier.ToString().ToUpper().AddModNamePrefix();
        card.Keywords.Add(kw);

        if (card.Keywords is not null)
        {
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