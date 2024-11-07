using CardmastersOfTamriel.ImageProcessor.Processors;
using CardmastersOfTamriel.ImageProcessor.Providers;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor;

public class Program
{
    private const string UpdateArg = "--updatemetadata";
    private const string ConvertArg = "--convert";
    private const string ReportArg = "--report";
    private const string ReplicateArg = "--replicatefolders";

    private static void Main(string[] args)
    {
        Log.Verbose($"User entered arguments {args}");

        if (args.Length == 0)
        {
            Log.Warning(
                "No arguments specified. Use:\n\t--analyze to record image sizes\n\t--convert to convert images\n\t--report to generate a report.");
            return;
        }

        var mode = args[0];
        if (mode != UpdateArg && mode != ConvertArg && mode != ReportArg && mode != ReplicateArg)
        {
            Log.Warning("Invalid mode specified.");
            return;
        }

        var config = ConfigurationProvider.Instance.Config;

        if (string.IsNullOrEmpty(config.Paths.OutputFolderPath) ||
            string.IsNullOrEmpty(config.Paths.MasterMetadataFilePath))
        {
            Log.Error("App config is missing");
            return;
        }

        SetupLogging(config);

        if (mode == ReplicateArg)
        {
            MapSourceFoldersToDestinationSets.BeginProcessing();
        }
        else
        {
            var processor = CreateProcessor(mode);
            if (processor is not null)
            {
                ImageProcessingCoordinator.BeginProcessing(processor);
            }
        }

        Log.CloseAndFlush();
    }

    private static void SetupLogging(Config config)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFilePath = Path.Combine(config.Paths.OutputFolderPath, "Logs", $"CardMastersOfTamriel_{timestamp}.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(logFilePath)
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();
    }

    private static ICardSetProcessor? CreateProcessor(string mode)
    {
        switch (mode)
        {
            case UpdateArg:
                Log.Information("Creating CardSetImageSizeProcessor");
                return new CardSetImageSizeProcessor();
            case ConvertArg:
                Log.Information("Creating CardSetImageProcessor");
                return new CardSetImageConversionProcessor();
            case ReportArg:
                Log.Information("Creating CardSetReportProcessor");
                return new CardSetReportProcessor();
            default:
                Log.Warning("Invalid mode specified.");
                return null;
        }
    }
}