using CardmastersOfTamriel.ImageProcessor.Processors;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Utilities;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor;

public class Program
{
    private const string AnalyzeArg = "--analyze";
    private const string ConvertArg = "--convert";

    private static void Main(string[] args)
    {
        Log.Verbose($"User entered arguments {args}");
        
        if (args.Length == 0)
        {
            Log.Warning("No arguments specified.");
            return;
        }

        var mode = args[0];
        if (mode != AnalyzeArg && mode != ConvertArg)
        {
            Log.Warning("Invalid mode specified.");
            return;
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("localsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var config = configuration.Get<Config>();
        if (config == null || string.IsNullOrEmpty(config.Paths.OutputFolderPath) ||
            string.IsNullOrEmpty(config.Paths.MasterMetadataFilePath))
        {
            Log.Error("App config is missing");
            return;
        }

        SetupLogging(config);

        var handler = new MasterMetadataHandler(config.Paths.MasterMetadataFilePath);
        ICardSetProcessor? processor = null;

        switch (mode)
        {
            case AnalyzeArg:
                processor = new CardSetImageSizeProcessor(config);
                Log.Information("Creating CardSetImageSizeProcessor");
                break;
            case ConvertArg:
            {
                processor = new CardSetProcessor(config, handler);
                Log.Information("Creating CardSetProcessor");
                break;
            }
        }

        if (processor is not null)
        {
            var imageProcessor = new ImageProcessingCoordinator(config, handler);
            imageProcessor.BeginProcessing(processor);
        }

        Log.CloseAndFlush();
    }

    private static void SetupLogging(Config config)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFilePath = Path.Combine(config.Paths.OutputFolderPath,
            $"CardMastersOfTamriel_{timestamp}.log");
        File.Delete(logFilePath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(logFilePath)
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();
    }
}