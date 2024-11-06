using CardmastersOfTamriel.SynthesisPatcher.Diagnostics;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;

namespace CardmastersOfTamriel.SynthesisPatcher.Utilities;

public static class LeveledItemExtensionMethods
{
    // Helper function to add a MiscItem to a leveled list
    public static void AddToLeveledItem(this LeveledItem leveledItem, MiscItem miscItem)
    {
        var entry = new LeveledItemEntry
        {
            Data = new LeveledItemEntryData
            {
                Reference = miscItem.ToLink(),
                Count = 1,
                Level = 1
            },
        };

        // Initialize the Entries list if it's null
        leveledItem.Entries ??= [];
        leveledItem.Entries?.Add(entry);

        ModificationTracker.IncrementLeveledItemEntryCount(leveledItem.EditorID ?? "UNKNOWN LL");
        // Log.Verbose($"Added MiscItem: {miscItem.EditorID} as LeveledItemEntry to LeveledItem: {leveledItem.EditorID}");
    }
}
