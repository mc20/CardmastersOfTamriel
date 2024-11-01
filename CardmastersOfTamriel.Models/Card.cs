namespace CardmastersOfTamriel.Models;

public class Card
{
    public string? Id { get; init; }
    public string? SetId { get; init; }
    public string? SetDisplayName { get; init; }
    public string? SeriesId { get; init; }
    public string? ImageFileName { get; init; }
    public string? ImageFilePath { get; init; }
    public CardShape Shape { get; init; }
    public string? DisplayName { get; init; }
    public int Index { get; init; }
    public int TotalCount { get; init; }
    public string? Description { get; init; }
    public CardTier Tier { get; init; }
    public uint Value { get; init; }
    public float Weight { get; init; }
    public string[]? Keywords { get; init; }

    // public MiscItem InsertAsMiscItem(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod)
    // {
    //     var newMiscItem = customMod.MiscItems.AddNew();
    //     newMiscItem.EditorID = $"MiscItem_Set_{SetId}_Card_{Id}".AddModNamePrefix();
    //     newMiscItem.Name = $"{SetDisplayName} - Card #{Index} of {TotalCount}";
    //     newMiscItem.Value = Value == 0 ? 10 : Value;
    //     newMiscItem.Weight = Weight;

    //     var textureSetForWorldModel = customMod.TextureSets.AddNew();
    //     textureSetForWorldModel.EditorID = $"TextureSet_Set_{SetId}_Card_{Id}".AddModNamePrefix();
    //     textureSetForWorldModel.Diffuse = @$"CardmastersOfTamriel\{ImageFilePath}";
    //     textureSetForWorldModel.NormalOrGloss = GetNormalOrGloss();
    //     //ITMNoteUp [SNDR:000C7A54]

    //     newMiscItem.Model = new Model()
    //     {
    //         File = GetModelForCard(),
    //         AlternateTextures = [new AlternateTexture()
    //                     {
    //                         Name = "Card",
    //                         Index = 0,
    //                         NewTexture = textureSetForWorldModel.ToLink()
    //                     }]
    //     };

    //     if (Keywords is not null && Keywords.Length > 0)
    //     {
    //         AddKeywordsToMiscItem(newMiscItem, state, Keywords);
    //     }

    //     return newMiscItem;
    // }

    // private static void AddKeywordsToMiscItem(MiscItem miscItem, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, params string[] keywordEditorIDs)
    // {
    //     // Initialize the Keywords list if it's null
    //     miscItem.Keywords ??= [];

    //     foreach (string keywordEditorID in keywordEditorIDs)
    //     {
    //         // Find the keyword by EditorID in the load order
    //         var keyword = state.LoadOrder.PriorityOrder.Keyword().WinningOverrides().FirstOrDefault(kw => kw.EditorID == keywordEditorID);

    //         if (keyword != null)
    //         {
    //             // Add the keyword to the MiscItem's Keywords list
    //             miscItem.Keywords.Add(keyword.ToLink());
    //             DebugTools.LogAction($"Added keyword {keywordEditorID} to {miscItem.EditorID}", LogMessageType.VERBOSE);
    //         }
    //         else
    //         {
    //             DebugTools.LogAction($"Keyword {keywordEditorID} not found in the load order.", LogMessageType.WARNING);
    //         }
    //     }
    // }
}
