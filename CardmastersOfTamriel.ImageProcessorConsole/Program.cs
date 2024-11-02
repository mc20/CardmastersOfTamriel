using CardmastersOfTamriel.SynthesisPatcher.Services;
using CardmastersOfTamriel.Utilities;
using Microsoft.Extensions.Configuration;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public class Program
{
    static async Task Main(string[] args)
    {
        // Load configuration from appsettings.json and other sources
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("localsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var appConfig = configuration.Get<AppConfig>();

        if (appConfig is null)
        {
            DebugTools.LogAction("Failed to load configuration.", LogMessageType.ERROR);
            return;
        }

        var loader = new MasterMetadataLoader(appConfig.OutputFolderPath);
        var masterMetadata = await loader.GetMasterMetadataAsync();

        var procesor = new MyProcessor(appConfig, masterMetadata);
        procesor.Start();

    }
}