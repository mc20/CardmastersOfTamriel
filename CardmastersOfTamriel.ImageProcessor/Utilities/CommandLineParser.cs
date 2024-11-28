namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public static class CommandLineParser
{
    private static readonly Dictionary<string, CommandMode> CommandMap = new()
    {
        ["--rebuild"] = CommandMode.Rebuild,
        ["--convert"] = CommandMode.Convert,
        ["--overridesets"] = CommandMode.OverrideSetData,
        ["--recompile"] = CommandMode.RecompileMasterMetadata,
        ["--updatecounts"] = CommandMode.UpdateCardSetCount,
        ["--passthrough"] = CommandMode.Passthrough
    };

    public static readonly Dictionary<CommandMode, string> CommandHelp = new()
    {
        [CommandMode.Convert] = "Convert images to DDS format",
        [CommandMode.Rebuild] = "Rebuild all metadata including seies, sets, and cards",
        [CommandMode.OverrideSetData] = "Replaces card metadata with specific override data",
        [CommandMode.RecompileMasterMetadata] = "Read all metadata files and recompile them into the master metadata file",
        [CommandMode.UpdateCardSetCount] = "Updates metadata to reflect change in card set sample size",
        [CommandMode.Passthrough] = "Starts the coordination but no processing is done on the sets",
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