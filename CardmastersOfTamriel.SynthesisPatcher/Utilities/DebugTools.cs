using CardmastersOfTamriel.SynthesisPatcher.Config;

namespace CardmastersOfTamriel.SynthesisPatcher;

public enum LogMessageType
{
    INFO,
    WARNING,
    ERROR,
    VERBOSE,
    EXCEPTION
}

public static class DebugTools
{
    public static void LogAction(string message, LogMessageType type = LogMessageType.INFO)
    {
        if (Globals.ShowConsoleOutput)
        {
            if (type == LogMessageType.VERBOSE && Globals.ShowVerbose)
            {
                Console.WriteLine($"[{type}]: {message}");
            }
            else if (type != LogMessageType.VERBOSE)
            {
                Console.WriteLine($"[{type}]: {message}");
            }
        }
    }

    public static void LogException(Exception ex, string additionalContextInfo = "")
    {
        LogAction(additionalContextInfo, LogMessageType.ERROR);
        LogAction(ex.ToString(), LogMessageType.EXCEPTION);
    }
}