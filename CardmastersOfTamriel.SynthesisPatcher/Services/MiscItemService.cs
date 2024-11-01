using CardmastersOfTamriel.Models;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

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

    public MiscItem InsertAsMiscItem(Card card)
    {
        var newMiscItem = _customMod.MiscItems.AddNew();
        newMiscItem.EditorID = $"MiscItem_Set_{card.SetId}_Card_{card.Id}".AddModNamePrefix();
        newMiscItem.Name = $"{card.SetDisplayName} - Card #{card.Index} of {card.TotalCount}";
        newMiscItem.Value = card.Value == 0 ? 10 : card.Value;
        newMiscItem.Weight = card.Weight;

        var textureSetForWorldModel = _customMod.TextureSets.AddNew();
        textureSetForWorldModel.EditorID = $"TextureSet_Set_{card.SetId}_Card_{card.Id}".AddModNamePrefix();
        textureSetForWorldModel.Diffuse = @$"CardmastersOfTamriel\{card.ImageFilePath}";
        textureSetForWorldModel.NormalOrGloss = card.GetNormalOrGloss();
        //ITMNoteUp [SNDR:000C7A54]

        newMiscItem.Model = new Model()
        {
            File = card.GetModelForCard(),
            AlternateTextures = [new AlternateTexture()
            {
                Name = "Card",
                Index = 0,
                NewTexture = textureSetForWorldModel.ToLink()
            }]
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

        foreach (string keywordEditorID in keywordEditorIDs)
        {
            // Find the keyword by EditorID in the load order
            var keyword = _state.LoadOrder.PriorityOrder.Keyword().WinningOverrides().FirstOrDefault(kw => kw.EditorID == keywordEditorID);

            if (keyword != null)
            {
                // Add the keyword to the MiscItem's Keywords list
                miscItem.Keywords.Add(keyword.ToLink());
                DebugTools.LogAction($"Added keyword {keywordEditorID} to {miscItem.EditorID}", LogMessageType.VERBOSE);
            }
            else
            {
                DebugTools.LogAction($"Keyword {keywordEditorID} not found in the load order.", LogMessageType.WARNING);
            }
        }
    }
}