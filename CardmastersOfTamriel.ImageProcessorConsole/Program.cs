using CardmastersOfTamriel.Utilities;
using Microsoft.Extensions.Configuration;

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

        var loader = new MasterMetadataLoader(appConfig.OutputFolderPath);
        var masterMetadata = loader.GetMasterMetadata();

        var processor = new ImageProcessor(appConfig, masterMetadata);
        processor.Start();

    }
}