using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Services;

public class MiscItemService : IMiscItemService
{
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _customMod;

    public MiscItemService(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod)
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
            if (miscItem is null) continue;
            miscItems.Add(card, miscItem);
            Log.Information($"Inserted MiscItem: {miscItem.EditorID}");
        }

        return miscItems;
    }

    private MiscItem? InsertAsMiscItem(Card card)
    {
        var newMiscItemId = $"MiscItem_Set_{card.SetId}_Card_{card.Id}".AddModNamePrefix();
        var newTextureSetId = $"TextureSet_Set_{card.SetId}_Card_{card.Id}".AddModNamePrefix();

        // Create a HashSet of existing EditorIDs for quick lookup

        if (_state.CheckIfExists<IMiscItemGetter>(newMiscItemId) || _customMod.CheckIfExists<MiscItem>(newMiscItemId)
        || _state.CheckIfExists<ITextureSetGetter>(newTextureSetId) || _customMod.CheckIfExists<TextureSet>(newTextureSetId))
        {
            Log.Warning($"MiscItem {newMiscItemId} already exists in the load order.");
            return default;
        }

        var newMiscItem = _customMod.MiscItems.AddNew();
        newMiscItem.EditorID = newMiscItemId;
        newMiscItem.Name = card.DisplayName;
        newMiscItem.Value = card.Value == 0 ? 10 : card.Value;
        newMiscItem.Weight = card.Weight;

        Counters.IncrementMiscItemCount(newMiscItem.EditorID);

        Log.Information($"_state.DataFolderPath: {_state.DataFolderPath}");

        var textureSetForWorldModel = _customMod.TextureSets.AddNew();
        textureSetForWorldModel.EditorID = newTextureSetId;
        textureSetForWorldModel.Diffuse = @$"CardmastersOfTamriel\{card.DestinationRelativeFilePath}";
        textureSetForWorldModel.NormalOrGloss = card.GetNormalOrGloss();
        //ITMNoteUp [SNDR:000C7A54]

        Log.Information($"Added TextureSet {textureSetForWorldModel.EditorID} with Diffuse Path: '{textureSetForWorldModel.Diffuse}'");

        Counters.IncrementTextureSetCount(textureSetForWorldModel.EditorID);

        newMiscItem.Model = new Model()
        {
            File = card.GetModelForCard(),
            AlternateTextures =
            [
                new AlternateTexture()
                {
                    Name = "Card",
                    Index = 0,
                    NewTexture = textureSetForWorldModel.ToLink()
                }
            ]
        };


        if (card.Keywords is not null && card.Keywords.Length > 0)
        {
            AddKeywordsToMiscItem(newMiscItem, card.Keywords);
        }

        return newMiscItem;
    }

    private void AddKeywordsToMiscItem(MiscItem miscItem, params string[] keywordEditorIDs)
    {
        // Initialize the Keywords list if it's null
        miscItem.Keywords ??= [];

        foreach (var keywordEditorId in keywordEditorIDs)
        {
            // Find the keyword by EditorID in the load order
            var keyword = _state.LoadOrder.PriorityOrder.Keyword().WinningOverrides()
                .FirstOrDefault(kw => kw.EditorID == keywordEditorId);

            if (keyword != null)
            {
                // Add the keyword to the MiscItem's Keywords list
                miscItem.Keywords.Add(keyword.ToLink());
                // Log.Verbose($"Added keyword {keywordEditorId} to {miscItem.EditorID}");
            }
            else
            {
                Log.Warning($"Keyword {keywordEditorId} not found in the load order.");
            }
        }
    }
}