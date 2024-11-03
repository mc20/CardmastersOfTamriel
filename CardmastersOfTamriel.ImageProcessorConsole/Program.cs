using CardmastersOfTamriel.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

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

        if (appConfig is null)
        {
            Logger.LogAction("Failed to load configuration.", LogMessageType.Error);
            return;
        }

        var serviceProvider = new ServiceCollection().AddLogging(config =>
        {
            config.AddConsole(); // Logs to the console
            config.AddDebug();   // Logs to the debug output
        })
        .AddSingleton(appConfig)           // Register AppConfig
        .AddTransient<ImageProcessor>()    // Register ImageProcessor
        .BuildServiceProvider();

        var logger = serviceProvider.GetService<ILogger<Program>>();

        MasterMetadataHandler.CreateInstance(appConfig.MasterMetadataPath);

        var processor = new ImageProcessor(appConfig);
        processor.Start();

    }
}