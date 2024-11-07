using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Diagnostics;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.MiscItems;

public class CardMiscItemCreator : ICardMiscItemCreator
{
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _customMod;

    public CardMiscItemCreator(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod)
    {
        _state = state;
        _customMod = customMod;
    }

    public Dictionary<Card, MiscItem> InsertAndMapCardsToMiscItems(IEnumerable<Card> cards)
    {
        var miscItems = new Dictionary<Card, MiscItem>();

        foreach (var card in cards)
        {
            var miscItem = InsertAsMiscItem(card);
            if (miscItem == null) continue;

            miscItems.Add(card, miscItem);
            Log.Information($"Inserted MiscItem: {miscItem.EditorID}");
        }

        return miscItems;
    }

    private MiscItem? InsertAsMiscItem(Card card)
    {
        var newMiscItemId = GenerateEditorId("MiscItem", card);
        var newTextureSetId = GenerateEditorId("TextureSet", card);

        if (ItemExists(newMiscItemId, newTextureSetId))
        {
            Log.Warning($"MiscItem {newMiscItemId} already exists in the load order.");
            return null;
        }

        var newMiscItem = CreateMiscItem(card, newMiscItemId);
        var textureSetForWorldModel = CreateTextureSet(card, newTextureSetId);

        newMiscItem.Model = CreateModel(card, textureSetForWorldModel);

        if (card.Keywords is not null)
        {
            AddKeywordsToMiscItem(newMiscItem, card.Keywords);
        }

        return newMiscItem;
    }

    private static string GenerateEditorId(string prefix, Card card)
    {
        return $"{prefix}_Set_{card.SetId}_Card_{card.Id}".AddModNamePrefix();
    }

    private bool ItemExists(string miscItemId, string textureSetId)
    {
        return _state.CheckIfExists<IMiscItemGetter>(miscItemId) || _customMod.CheckIfExists<MiscItem>(miscItemId)
                                                                 || _state.CheckIfExists<ITextureSetGetter>(
                                                                     textureSetId) ||
                                                                 _customMod.CheckIfExists<TextureSet>(textureSetId);
    }

    private MiscItem CreateMiscItem(Card card, string editorId)
    {
        var newMiscItem = _customMod.MiscItems.AddNew();
        newMiscItem.EditorID = editorId;
        newMiscItem.Name = card.DisplayName;
        newMiscItem.Value = card.Value == 0 ? 10 : card.Value;
        newMiscItem.Weight = card.Weight;

        ModificationTracker.IncrementMiscItemCount(newMiscItem.EditorID);

        return newMiscItem;
    }

    private TextureSet CreateTextureSet(Card card, string editorId)
    {
        var textureSet = _customMod.TextureSets.AddNew();
        textureSet.EditorID = editorId;
        textureSet.Diffuse = @$"CardmastersOfTamriel\{card.DestinationRelativeFilePath}";
        textureSet.NormalOrGloss = card.GetNormalOrGloss();

        Log.Information($"Added TextureSet {textureSet.EditorID} with Diffuse Path: '{textureSet.Diffuse}'");

        ModificationTracker.IncrementTextureSetCount(textureSet.EditorID);

        return textureSet;
    }

    private static Model CreateModel(Card card, TextureSet textureSet)
    {
        return new Model
        {
            File = card.GetModelForCard(),
            AlternateTextures =
            [
                new AlternateTexture
                {
                    Name = "Card",
                    Index = 0,
                    NewTexture = textureSet.ToLink()
                }
            ]
        };
    }

    private void AddKeywordsToMiscItem(MiscItem miscItem, HashSet<string> keywordEditorIDs)
    {
        miscItem.Keywords ??= [];
        foreach (var keywordEditorId in keywordEditorIDs)
        {
            var keyword = _state.LoadOrder.PriorityOrder.Keyword().WinningOverrides()
                .FirstOrDefault(kw => kw.EditorID == keywordEditorId);

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