using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.MiscItems.Factory;

public static class MiscItemFactory
{
    public static MiscItem CreateMiscItem(ISkyrimMod skyrimMod, Card card)
    {
        var editorId = $"MiscItem_{card.Id}".AddModNamePrefix();
        var newMiscItem = skyrimMod.MiscItems.AddNewWithId(editorId);
        newMiscItem.Name = card.DisplayName;
        newMiscItem.Value = card.Value;
        newMiscItem.Weight = card.Weight;

        Log.Debug($"Added MiscItem {newMiscItem.EditorID} with Name: '{newMiscItem.Name}'");

        return newMiscItem;
    }
}