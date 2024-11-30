namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public static class CommandLineParser
{
    private static readonly Dictionary<string, CommandMode> CommandMap = new()
    {
        ["--convert"] = CommandMode.Convert,
        ["--recompile"] = CommandMode.RecompileMasterMetadata,
        ["--passthrough"] = CommandMode.Passthrough,
        ["--override"] = CommandMode.Override
    };

    public static readonly Dictionary<CommandMode, string> CommandHelp = new()
    {
        [CommandMode.Convert] = "Convert images to DDS format and create metadata files.",
        [CommandMode.RecompileMasterMetadata] = "Read all metadata files and recompile them into the master metadata file.",
        [CommandMode.Passthrough] = "Starts the coordination but no processing is done on the sets.",
        [CommandMode.Override] = "If available, applies overrides to set card data."
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
        foreach (var (arg, mode) in CommandMap) Console.WriteLine($"{arg,-20} {CommandHelp[mode]}");
    }
}