using CardmastersOfTamriel.SynthesisPatcher.Diagnostics;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.MiscItems.Factory;

public static class MiscItemFactory
{
    public static MiscItem CreateMiscItem(ISkyrimMod skyrimMod, Card card, FormKey formKey)
    {
        var newMiscItem = skyrimMod.MiscItems.AddNew(formKey);
        newMiscItem.Name = card.DisplayName;
        newMiscItem.Value = card.Value == 0 ? 10 : card.Value;
        newMiscItem.Weight = card.Weight;

        Log.Verbose($"Added MiscItem {newMiscItem.EditorID} with Name: '{newMiscItem.Name}'");

        ModificationTracker.IncrementMiscItemCount(newMiscItem.FormKey.ToString());

        return newMiscItem;
    }
}