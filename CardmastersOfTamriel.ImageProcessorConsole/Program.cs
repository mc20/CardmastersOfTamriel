using System.Reflection;
using CardmastersOfTamriel.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

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
        if (appConfig == null || string.IsNullOrEmpty(appConfig.OutputFolderPath) || string.IsNullOrEmpty(appConfig.MasterMetadataPath))
        {
            Log.Error("App config is missing");
            return;
        }

        // reset the log
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFilePath = Path.Combine(appConfig.OutputFolderPath, $"CardMastersOfTamriel_{timestamp}.txt");
        File.Delete(logFilePath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(logFilePath)
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();

        var imageProcessor = new ImageProcessor(appConfig, new MasterMetadataHandler(appConfig.MasterMetadataPath));
        imageProcessor.Start();

        Log.CloseAndFlush();
    }
}