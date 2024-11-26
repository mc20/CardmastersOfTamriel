using System.Collections.Concurrent;
using CardmastersOfTamriel.ImageProcessor.CardSets;
using CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor;

public class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            var cts = new CancellationTokenSource();

            var config = GetConfiguration();

            if (!Directory.Exists(config?.Paths?.OutputFolderPath))
            {
                var ex = new InvalidOperationException($"Output folder does not exist: '{config?.Paths?.OutputFolderPath}'");
                Console.WriteLine(ex.ToString());
                Log.Fatal(ex, "Exiting");
                throw ex;
            }

            // Relies on OutputFolderPath being set
            SetupLogging(config);

            Log.Verbose($"User entered arguments {string.Join(", ", args)}");

            if (!CommandLineParser.TryParseCommand(args, out var mode))
            {
                var ex = new InvalidOperationException("Invalid or missing command");
                Log.Fatal(ex, "Exiting");
                throw ex;
            }

            await ExecuteCommand(mode, config);

            Log.Information("Processing complete.");

            if (!JsonFileReader.InvalidJsonFilesDictionary.IsEmpty)
            {
                Log.Warning("Invalid JSON files:");
                foreach (var (filePath, message) in JsonFileReader.InvalidJsonFilesDictionary)
                {
                    Log.Warning($"  {filePath}: {message}");
                }
            }
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

    private static Config GetConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("localsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        return configuration.Get<Config>() ?? throw new InvalidOperationException("Failed to load configuration.");
    }

    private static void SetupLogging(Config config)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFilePath = Path.Combine(config.Paths.OutputFolderPath, "Logs", $"CardMastersOfTamriel_{timestamp}.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            // .WriteTo.Console()
            .WriteTo.File(logFilePath)
            .WriteTo.Debug()
            .CreateLogger();
    }

    private static async Task ExecuteCommand(CommandMode mode, Config config)
    {
        ICardSetHandler? handler = mode switch
        {
            CommandMode.Convert => new CardSetImageConversionHandler(config),
            CommandMode.Rebuild => new RebuildMasterMetadataHandler(config),
            CommandMode.OverrideSetData => new OverrideSetMetadataHandler(),
            CommandMode.RecompileMasterMetadata => new CompileMasterMetadataHandler(),
            CommandMode.UpdateCardSetCount => new ChangeNumberOfCardsInSetHandler(config),
            _ => null,
        };

        if (handler is not null)
        {
            var cts = new CancellationTokenSource();

            var overrides = await LoadOverridesAsync(config.Paths.SetMetadataOverrideFilePath, cts.Token);

            var coordinator = new ImageProcessingCoordinator(config, cts.Token, overrides);
            await coordinator.PerformProcessingUsingHandlerAsync(handler);
            await coordinator.CleanupNonTrackedFilesAtDestination();
            await coordinator.CompileSeriesMetadataAsync();

            await JsonFileWriter.WriteToJsonAsync(overrides, config.Paths.SetMetadataOverrideFilePath, cts.Token);
        }
        else
        {
            Log.Error("Invalid command");
        }
    }

    private static async Task<ConcurrentDictionary<string, CardSeriesBasicMetadata>> LoadOverridesAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var overrides = await JsonFileReader.ReadFromJsonAsync<Dictionary<string, CardSeriesBasicMetadata>>(filePath, cancellationToken);
                return new ConcurrentDictionary<string, CardSeriesBasicMetadata>(overrides);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to load overrides from '{filePath}'");
        }

        return [];
    }
}