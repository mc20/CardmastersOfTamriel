namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public static class CommandLineParser
{
    private static readonly Dictionary<string, CommandMode> CommandMap = new()
    {
        ["--rebuild"] = CommandMode.Rebuild,
        ["--convert"] = CommandMode.Convert,
        ["--overridesets"] = CommandMode.OverrideSetData,
        ["--recompile"] = CommandMode.RecompileMasterMetadata,
        ["--updatecounts"] = CommandMode.UpdateCardSetCount
    };

    public static readonly Dictionary<CommandMode, string> CommandHelp = new()
    {
        [CommandMode.Convert] = "Convert images to DDS format",
        [CommandMode.Rebuild] = "Rebuild all metadata",
        [CommandMode.OverrideSetData] = "Refresh card sets",
        [CommandMode.RecompileMasterMetadata] = "Recompile master metadata",
        [CommandMode.UpdateCardSetCount] = "Updates metadata to reflect change in card set sample size"
    };

    public static bool TryParseCommand(string[] args, out CommandMode mode)
    {
        mode = default;

        if (args.Length != 0) return CommandMap.TryGetValue(args[0], out mode);
        PrintHelp();
        return false;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("\nAvailable commands:");
        foreach (var (arg, mode) in CommandMap)
        {
            Console.WriteLine($"{arg,-20} {CommandHelp[mode]}");
        }
    }
}