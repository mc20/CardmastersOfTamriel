using CardmastersOfTamriel.Models;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Utilities;

public static class Extensions
{
    public static string AddModNamePrefix(this string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return str;

        return $"CardmastersOfTamriel_{str}";
    }

    public static string GetModelForCard(this Card card) => card.Shape switch
    {
        CardShape.Portrait => @"CardmastersOfTamriel\CardBasic_Portrait.nif",
        CardShape.Landscape => @"CardmastersOfTamriel\CardBasic_Landscape.nif",
        CardShape.Square => @"CardmastersOfTamriel\CardBasic_Square.nif",
        _ => @"CardmastersOfTamriel\CardBasic_Portrait.nif"
    };

    public static string GetNormalOrGloss(this Card card) => card.Shape switch
    {
        CardShape.Portrait => @"CardmastersOfTamriel\CardPortrait_n.dds",
        CardShape.Landscape => @"CardmastersOfTamriel\CardLandscape_n.dds",
        CardShape.Square => @"CardmastersOfTamriel\CardSquare_n.dds",
        _ => @"CardmastersOfTamriel\Card_n.dds"
    };

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

        Log.Verbose($"Adding MiscItem: {miscItem.EditorID} to LeveledItem: {leveledItem.EditorID}");
    }
}
