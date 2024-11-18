namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public static class CommandLineParser
{
    private static readonly Dictionary<string, CommandMode> CommandMap = new()
    {
        ["--rebuild"] = CommandMode.Rebuild,
        ["--rebuildasync"] = CommandMode.RebuildAsync,
        ["--convert"] = CommandMode.Convert,
        ["--convertasync"] = CommandMode.ConvertAsync,
        ["--report"] = CommandMode.Report,
        ["--overridesets"] = CommandMode.OverrideSetData
        // ["--replicate"] = CommandMode.Replicate,
        // ["--update"] = CommandMode.Update,
    };

    private static readonly Dictionary<CommandMode, string> CommandHelp = new()
    {
        [CommandMode.Convert] = "Convert images to DDS format",
        [CommandMode.Report] = "Generate a metadata report",
        [CommandMode.Rebuild] = "Rebuild all metadata",
        [CommandMode.RebuildAsync] = "Rebuild all metadata asynchronously",
        [CommandMode.OverrideSetData] = "Refresh card sets"
        // [CommandMode.Replicate] = "Replicate folder structure",
        // [CommandMode.Update] = "Update master metadata",
    };

    public static bool TryParseCommand(string[] args, out CommandMode mode)
    {
        mode = default;

        if (args.Length == 0)
        {
            PrintHelp();
            return false;
        }

        return CommandMap.TryGetValue(args[0], out mode);
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
