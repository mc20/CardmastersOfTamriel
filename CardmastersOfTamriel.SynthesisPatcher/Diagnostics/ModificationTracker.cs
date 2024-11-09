using Serilog;
using System.Runtime.CompilerServices;

namespace CardmastersOfTamriel.SynthesisPatcher.Diagnostics;

public static class ModificationTracker
{
    private static int MiscItemInsertCount { get; set; } = 0;
    private static List<string> MiscItemInsertLocations { get; set; } = [];

    private static int TextureSetInsertCount { get; set; } = 0;
    private static List<string> TextureSetInsertLocations { get; set; } = [];

    private static int LeveledItemInsertCount { get; set; }
    private static List<string> LeveledItemInsertLocations { get; set; } = [];

    private static int LeveledItemEntryInsertCount { get; set; }
    private static List<string> LeveledItemEntryInsertLocations { get; set; } = [];

    public static void IncrementMiscItemCount(string context, [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        MiscItemInsertCount++;
        MiscItemInsertLocations.Add($"{callerFilePath} {callerName} line:{callerLineNumber}");
        // Log.Verbose($"Incremented MiscItem to {MiscItemInsertCount:D3} from {Path.GetFileName(callerFilePath)}.{callerName} (line:{callerLineNumber}):\t{context}");
    }

    public static void IncrementTextureSetCount(string context, [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        TextureSetInsertCount++;
        TextureSetInsertLocations.Add($"{callerName} {callerFilePath} {callerLineNumber}");
        // Log.Verbose($"Incremented TextureSet to {TextureSetInsertCount:D3} from {Path.GetFileName(callerFilePath)}.{callerName} (line:{callerLineNumber}):\t{context}");
    }

    public static void IncrementLeveledItemCount(string context, [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        LeveledItemInsertCount++;
        LeveledItemInsertLocations.Add($"{callerName} {callerFilePath} {callerLineNumber}");
        // Log.Verbose($"Incremented LeveledItem to {LeveledItemInsertCount:D3} from {Path.GetFileName(callerFilePath)}.{callerName} (line:{callerLineNumber}):\t{context}");
    }

    public static void IncrementLeveledItemEntryCount(string context, [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        LeveledItemEntryInsertCount++;
        LeveledItemEntryInsertLocations.Add($"{callerName} {callerFilePath} {callerLineNumber}");
        // Log.Verbose($"Incremented LeveledItemEntry to {LeveledItemEntryInsertCount:D3} from {Path.GetFileName(callerFilePath)}.{callerName} (line:{callerLineNumber}):\t{context}");
    }

    public static void PrintToLog()
    {
        Log.Information($"MiscItemInsertCount: {MiscItemInsertCount}, Locations: \n{string.Join(",\n", MiscItemInsertLocations)}");
        Log.Information($"TextureSetInsertCount: {TextureSetInsertCount}, Locations: \n{string.Join(",\n", TextureSetInsertLocations)}");
        Log.Information($"LeveledItemInsertCount: {LeveledItemInsertCount}, Locations: \n{string.Join(",\n", LeveledItemInsertLocations)}");
        Log.Information($"LeveledItemEntryInsertCount: {LeveledItemEntryInsertCount}, \nLocations: {string.Join(",\n", LeveledItemEntryInsertLocations)}");
    }
}
