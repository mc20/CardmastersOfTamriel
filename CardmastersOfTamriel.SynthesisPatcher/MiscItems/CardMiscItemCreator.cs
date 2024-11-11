using CardmastersOfTamriel.SynthesisPatcher.MiscItems.Factory;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.MiscItems;

public class CardMiscItemCreator
{
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _customMod;
    private readonly FormIdGenerator _formIdGenerator;

    public CardMiscItemCreator(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod,
        FormIdGenerator formIdGenerator)
    {
        _state = state;
        _customMod = customMod;
        _formIdGenerator = formIdGenerator;
    }

    public Dictionary<Card, MiscItem> InsertAndMapCardsToMiscItems(HashSet<Card> cards)
    {
        var miscItems = new Dictionary<Card, MiscItem>();

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
        var newMiscItemFormKey = GenerateFormKey("MiscItem", card);
        var newTextureSetFormKey = GenerateFormKey("TextureSet", card);

        if (ItemExists(newMiscItemFormKey, newTextureSetFormKey))
        {
            Log.Error($"MiscItem '{newMiscItemFormKey}' already exists in the load order.");
            return null;
        }

        var newMiscItem = MiscItemFactory.CreateMiscItem(_customMod, card, newMiscItemFormKey);
        var textureSetForWorldModel = TextureSetFactory.CreateTextureSet(_customMod, card, newTextureSetFormKey);

        newMiscItem.Model = ModelFactory.CreateModel(card, textureSetForWorldModel);

        card.Keywords ??= [];

        if (card.Keywords is not null)
        {
            AddKeywordsToMiscItem(newMiscItem, card.Keywords);
        }

        Log.Verbose($"Card: {card.Id} '{card.DisplayName}' inserted as MiscItem: {newMiscItem.EditorID}");

        return newMiscItem;
    }

    private FormKey GenerateFormKey(string prefix, Card card)
    {
        return _formIdGenerator.GetNextFormKey($"{prefix}_CARD_{card.Id}".AddModNamePrefix());
    }

    private bool ItemExists(FormKey miscItemFormKey, FormKey textureSetFormKey)
    {
        return _state.CheckIfExists<IMiscItemGetter>(miscItemFormKey) || _customMod.CheckIfExists<MiscItem>(
                                                                          miscItemFormKey)
                                                                      || _state.CheckIfExists<ITextureSetGetter>(
                                                                          textureSetFormKey)
                                                                      || _customMod.CheckIfExists<TextureSet>(
                                                                          textureSetFormKey);
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