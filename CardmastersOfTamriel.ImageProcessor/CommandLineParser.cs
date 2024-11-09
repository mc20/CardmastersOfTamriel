namespace CardmastersOfTamriel.ImageProcessor;

public class CommandLineParser
{
    private static readonly Dictionary<string, CommandMode> CommandMap = new()
    {
        ["--update"] = CommandMode.Update,
        ["--rebuild"] = CommandMode.Rebuild,
        ["--convert"] = CommandMode.Convert,
        ["--report"] = CommandMode.Report,
        ["--replicate"] = CommandMode.Replicate,
        ["--overridesets"] = CommandMode.OverrideSetData
    };

    private static readonly Dictionary<CommandMode, string> CommandHelp = new()
    {
        [CommandMode.Convert] = "Convert images to DDS format",
        [CommandMode.Report] = "Generate a metadata report",
        [CommandMode.Update] = "Update master metadata",
        [CommandMode.Rebuild] = "Rebuild all metadata",
        [CommandMode.Replicate] = "Replicate folder structure",
        [CommandMode.OverrideSetData] = "Refresh card sets"
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
