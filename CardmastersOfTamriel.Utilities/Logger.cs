namespace CardmastersOfTamriel.Utilities
{
    [Obsolete("Use Serilog instead", true)]
    public enum LogMessageType
    {
        Info,
        Warning,
        Error,
        Verbose,
        Exception
    }

    [Obsolete("Use Serilog instead", true)]
    public static class Logger
    {
        // Configurable minimum log level
        public static LogMessageType MinimumLogLevel { get; set; } = LogMessageType.Info;

        public static void LogAction(string message, LogMessageType type = LogMessageType.Info)
        {
            // Check if the log message should be displayed based on the settings
            if (Globals.ShowConsoleOutput && 
                (int)type >= (int)MinimumLogLevel && 
                (type != LogMessageType.Verbose || Globals.ShowVerbose))
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{type}]: {message}");
            }
        }

        public static void LogException(Exception ex, string additionalContextInfo = "")
        {
            // Construct detailed exception message
            var exceptionDetails = $"Exception: {ex.Message}\nStackTrace: {ex.StackTrace}";
            if (!string.IsNullOrEmpty(additionalContextInfo))
            {
                exceptionDetails = $"Context: {additionalContextInfo}\n{exceptionDetails}";
            }
            LogAction(exceptionDetails, LogMessageType.Exception);
        }

        // Method to log messages to a file (extend as needed)
        public static void LogToFile(string message, string filePath)
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            File.AppendAllText(filePath, logMessage + Environment.NewLine);
        }
    }
}