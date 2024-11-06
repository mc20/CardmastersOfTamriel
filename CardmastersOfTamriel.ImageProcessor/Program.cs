using CardmastersOfTamriel.ImageProcessorConsole.Processors;
using CardmastersOfTamriel.Utilities;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public class Program
{
    static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("localsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var config = configuration.Get<Config>();
        if (config == null || string.IsNullOrEmpty(config.Paths.OutputFolderPath) || string.IsNullOrEmpty(config.Paths.MasterMetadataFilePath))
        {
            Log.Error("App config is missing");
            return;
        }

        SetupLogging(config);

        var imageProcessor = new ImageProcessingCoordinator(config, new MasterMetadataHandler(config.Paths.MasterMetadataFilePath));
        imageProcessor.BeginProcessing();

        Log.CloseAndFlush();
    }

    private static void SetupLogging(Config config)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFilePath = Path.Combine(config.Paths.OutputFolderPath ?? string.Empty, $"CardMastersOfTamriel_{timestamp}.log");
        File.Delete(logFilePath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(logFilePath)
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();
    }
}