using System.Text.Json;
using CardmastersOfTamriel.ImageProcessor.CardSets;
using CardmastersOfTamriel.ImageProcessor.Processors;
using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor;

public class Program
{
    private static async Task Main(string[] args)
    {
        var config = ConfigurationProvider.Instance.Config;

        if (!Directory.Exists(config?.Paths?.OutputFolderPath))
        {
            Log.Error($"Output folder does not exist: '{config?.Paths?.OutputFolderPath}'");
            return;
        }

        // Relies on OutputFolderPath being set
        SetupLogging(config);

        if (!File.Exists(config?.Paths?.MasterMetadataFilePath))
        {
            Log.Warning($"Master metadata file does not exist: '{config?.Paths?.MasterMetadataFilePath}', creating a new one.");
            if (config?.Paths?.MasterMetadataFilePath != null)
            {
                var defaultMetadata = new
                {
                    Sets = new List<object>()
                };

                var json = JsonSerializer.Serialize(defaultMetadata, JsonSettings.Options);
                await File.WriteAllTextAsync(config.Paths.MasterMetadataFilePath, json);
            }
            else
            {
                Log.Error("Master metadata file path is null.");
                return;
            }
        }

        Log.Verbose($"User entered arguments {string.Join(", ", args)}");

        if (!CommandLineParser.TryParseCommand(args, out var mode))
        {
            Log.Warning("Invalid or missing command");
            return;
        }

        ExecuteCommand(mode);

        Log.Information("Processing complete.");

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

    private static void ExecuteCommand(CommandMode mode)
    {
        ICardSetHandler? command = null;

        switch (mode)
        {
            case CommandMode.Convert:
                command = new CardSetImageConversionProcessor();
                break;
            case CommandMode.Report:
                command = new CardSetReportProcessor();
                break;
            case CommandMode.Update:
                // Update metadata logic
                break;
            case CommandMode.Rebuild:
                command = new RebuildMasterMetadata();
                break;
            case CommandMode.Replicate:
                // Replicate folders logic
                //MapSourceFoldersToDestinationSets.BeginProcessing();
                break;
            case CommandMode.OverrideSetData:
                command = new OverrideSetMetadata();
                break;
            default:
                command = null;
                break;
        }

        if (command is not null)
        {
            ImageProcessingCoordinator.BeginProcessing(command);
        }
        else
        {
            Log.Error("Invalid command");
        }
    }
}