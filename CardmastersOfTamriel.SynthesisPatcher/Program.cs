using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Microsoft.Extensions.Configuration;
using CardmastersOfTamriel.SynthesisPatcher.Services;
using CardmastersOfTamriel.SynthesisPatcher.Models;
using CardmastersOfTamriel.Utilities;
using Mutagen.Bethesda.Plugins;
using Serilog;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Noggog;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins.Binary.Parameters;

namespace CardmastersOfTamriel.SynthesisPatcher;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await SynthesisPipeline.Instance
            .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
            .SetTypicalOpen(GameRelease.SkyrimSE, "CardmastersOfTamriel.esp")
            .Run(args);
    }

    private static void SetupLogging(AppConfig appConfig, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        // var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFilePath = appConfig.RetrieveLogFilePath(state);
        File.Delete(logFilePath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(logFilePath)
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();
    }

    private static async Task RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        var customMod = new SkyrimMod(new ModKey("CardmastersOfTamriel", ModType.Plugin), SkyrimRelease.SkyrimSE);

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(state.RetrieveInternalFilePath("appsettings.json"), optional: false, reloadOnChange: true)
            .AddJsonFile(state.RetrieveInternalFilePath("localsettings.json"), optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var appConfig = configuration.Get<AppConfig>();

        if (appConfig is null || string.IsNullOrEmpty(appConfig.LogOutputFilePath) || string.IsNullOrEmpty(appConfig.MetadataFilePath))
        {
            Log.Error("App config is missing");
            return;
        }

        SetupLogging(appConfig, state);

        var metadataHandler = new MasterMetadataHandler(appConfig.RetrieveMetadataFilePath(state));
        metadataHandler.LoadFromFile();

        var npcCollectorService = new CollectorFactory(appConfig.RetrieveCollectorNpcConfigFilePath(state));
        var containerCollectorService = new CollectorFactory(appConfig.RetrieveCollectorContainerConfigPath(state));

        // Create collectors for each CollectorType
        var npcCollectors = Enum.GetValues(typeof(CollectorType))
            .Cast<CollectorType>()
            .Select(npcCollectorService.CreateCollector)
            .ToList();

        // Create collectors for each CollectorType
        var containerCollectors = Enum.GetValues(typeof(CollectorType))
            .Cast<CollectorType>()
            .Select(containerCollectorService.CreateCollector)
            .ToList();

        var helper = new MetadataHelper(metadataHandler);
        var cardList = helper.GetCards();

        if (!cardList.Any()) return;

        var miscService = new MiscItemService(state, customMod);
        var mappedMiscItems = miscService.InsertAndMapCardsToMiscItems(cardList);

        var cardTierItemCreator = new CardTierLeveledItemCreator(state, customMod);
        var cardTierMappings = cardTierItemCreator.CreateLeveledItemsForCardTiers(mappedMiscItems);

        Log.Information(string.Empty);
        Log.Information("\n\nAssigning MiscItems to Collector LeveledItems..\n");

        var itemProcessor = new CollectorItemProcessor(appConfig, state, customMod);
        itemProcessor.SetupCollectorLeveledEntries(npcCollectors, cardTierMappings);
        itemProcessor.SetupCollectorLeveledEntries(containerCollectors, cardTierMappings);

        using var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
        var desiredFilePath = Path.Combine(env.DataFolderPath, "CardmastersOfTamriel.esp");

        customMod.WriteToBinary(
            desiredFilePath,
            new BinaryWriteParameters() { MastersListOrdering = new MastersListOrderingByLoadOrder(state.LoadOrder) });

        Log.Information("Mod successfully created and written to disk.");

        Log.CloseAndFlush();
    }
}