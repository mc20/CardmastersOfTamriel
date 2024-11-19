using System.Collections.Concurrent;
using CardmastersOfTamriel.ImageProcessor.CardSets;
using CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;
using CardmastersOfTamriel.ImageProcessor.Processors;
using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
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
            .MinimumLevel.Information()
            .WriteTo.File(logFilePath)
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();
    }

    private static async Task ExecuteCommand(CommandMode mode)
    {
        ICardSetHandler? asyncCommand = mode switch
        {
            CommandMode.Convert => new CardSetImageConversionHandler(),
            CommandMode.Rebuild => new RebuildMasterMetadataHandler(),
            CommandMode.OverrideSetData => new OverrideSetMetadataHandler(),
            _ => null,
        };

        if (asyncCommand is not null)
        {
            var cts = new CancellationTokenSource();
            await ImageProcessingCoordinator.BeginProcessingAsync(asyncCommand, cts.Token);
        }
        else
        {
            Log.Error("Invalid command");
        }
    }
}

public class ProgressEventArgs : EventArgs
{
    public CardTier Tier { get; }
    public string SetId { get; }

    public ProgressEventArgs(CardTier tier, string setId)
    {
        Tier = tier;
        SetId = setId;
    }
}

public class ProgressManager
{
    private readonly ConcurrentDictionary<CardTier, int> _progress = new();

    public void OnProgressUpdated(object sender, ProgressEventArgs e)
    {
        _progress[e.Tier]++;

        lock (_progress)
        {
            Console.SetCursorPosition(0, Array.IndexOf(_progress.Keys.ToArray(), e.Tier));
            Console.WriteLine($"{e.Tier}: {_progress[e.Tier]}");
        }
    }
}