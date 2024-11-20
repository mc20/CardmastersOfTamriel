using CardmastersOfTamriel.ImageProcessor.CardSets;
using CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;
using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor;

public class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            var cts = new CancellationTokenSource();

            var config = ConfigurationProvider.Instance.Config;

            if (!Directory.Exists(config?.Paths?.OutputFolderPath))
            {
                Log.Error($"Output folder does not exist: '{config?.Paths?.OutputFolderPath}'");
                return;
            }

            // Relies on OutputFolderPath being set
            SetupLogging(config);

            Log.Verbose($"User entered arguments {string.Join(", ", args)}");

            if (!CommandLineParser.TryParseCommand(args, out var mode))
            {
                Log.Warning("Invalid or missing command");
                return;
            }

            await ExecuteCommand(mode);

            Log.Information("Processing complete.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static void SetupLogging(Config config)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFilePath = Path.Combine(config.Paths.OutputFolderPath, "Logs", $"CardMastersOfTamriel_{timestamp}.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logFilePath)
            .WriteTo.Debug()
            .CreateLogger();
    }

    private static async Task ExecuteCommand(CommandMode mode)
    {
        ICardSetHandler? handler = mode switch
        {
            CommandMode.Convert => new CardSetImageConversionHandler(),
            CommandMode.Rebuild => new RebuildMasterMetadataHandler(),
            CommandMode.OverrideSetData => new OverrideSetMetadataHandler(),
            CommandMode.RecompileMasterMetadata => new CompileMasterMetadataHandler(),
            _ => null,
        };

        if (handler is not null)
        {
            var cts = new CancellationTokenSource();

            var coordinator = new ImageProcessingCoordinator();

            await coordinator.PerformProcessingUsingHandlerAsync(handler, cts.Token);
        }
        else
        {
            Log.Error("Invalid command");
        }
    }
}