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

        var appConfig = configuration.Get<AppConfig>();
        if (appConfig == null || string.IsNullOrEmpty(appConfig.OutputFolderPath) || string.IsNullOrEmpty(appConfig.MasterMetadataFilePath))
        {
            Log.Error("App config is missing");
            return;
        }

        SetupLogging(appConfig);

        var imageProcessor = new ImageProcessingCoordinator(appConfig, new MasterMetadataHandler(appConfig.MasterMetadataFilePath));
        imageProcessor.BeginProcessing();

        Log.CloseAndFlush();
    }

    private static void SetupLogging(AppConfig appConfig)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFilePath = Path.Combine(appConfig.OutputFolderPath ?? string.Empty, $"CardMastersOfTamriel_{timestamp}.log");
        File.Delete(logFilePath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(logFilePath)
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();
    }
}