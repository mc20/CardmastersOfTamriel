namespace CardmastersOfTamriel.Utilities;

public static class Globals
{
    public static readonly bool ShowConsoleOutput = true;
    public static readonly bool ShowVerbose = true;
    public static string ProjectDataDirectoryName => Path.Join(Directory.GetParent(Directory.GetCurrentDirectory())?.FullName ?? "", "Synthesis Patchers", "CardmastersOfTamriel", "CardmastersOfTamriel.SynthesisPatcher", "Data");
}