using CardmastersOfTamriel.Models;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Microsoft.Extensions.Configuration;
using CardmastersOfTamriel.SynthesisPatcher.Models;
using CardmastersOfTamriel.Utilities;
using Mutagen.Bethesda.Plugins;
using Serilog;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using CardmastersOfTamriel.SynthesisPatcher.MiscItems;
using CardmastersOfTamriel.SynthesisPatcher.LeveledItems;
using CardmastersOfTamriel.SynthesisPatcher.Configuration;

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

    private static async Task RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        var customMod = new SkyrimMod(new ModKey("CardmastersOfTamriel", ModType.Plugin), SkyrimRelease.SkyrimSE);

        var configuration = SetupConfiguration(state);

        var appConfig = configuration.Get<AppConfig>();

        if (appConfig is null || string.IsNullOrEmpty(appConfig.LogOutputFilePath) || string.IsNullOrEmpty(appConfig.MetadataFilePath))
        {
            Log.Error("App config is missing");
            return;
        }

        SetupLogging(appConfig, state);

        var metadataHandler = new MasterMetadataHandler(appConfig.RetrieveMetadataFilePath(state));
        metadataHandler.LoadFromFile();

        var (npcCollectors, containerCollectors) = SetupCollectors(appConfig, state);

        var helper = new MetadataHelper(metadataHandler);
        var cardList = helper.GetCards().ToList();

        if (cardList.Count == 0) return;

        var miscService = new CardMiscItemCreator(state, customMod);
        var mappedMiscItems = miscService.InsertAndMapCardsToMiscItems(cardList);

        var cardTierItemCreator = new TieredCardLeveledItemAssembler(state, customMod);
        var cardTierMappings = cardTierItemCreator.CreateCardTierLeveledItems(mappedMiscItems);

        Log.Information(string.Empty);
        Log.Information("\n\nAssigning MiscItems to Collector LeveledItems..\n");

        var itemProcessor = new CollectorLeveledItemDistributor(appConfig, state, customMod);
        itemProcessor.SetupCollectorLeveledEntries(npcCollectors, cardTierMappings);
        itemProcessor.SetupCollectorLeveledEntries(containerCollectors, cardTierMappings);

        using var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
        var desiredFilePath = Path.Combine(env.DataFolderPath, "CardmastersOfTamriel.esp");

        customMod.WriteToBinary(
            desiredFilePath,
            new BinaryWriteParameters() { MastersListOrdering = new MastersListOrderingByLoadOrder(state.LoadOrder) });

        Log.Information("Mod successfully created and written to disk.");

        await Log.CloseAndFlushAsync();
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
    
    private static IConfigurationRoot SetupConfiguration(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        return new ConfigurationBuilder()
            .AddJsonFile(state.RetrieveInternalFilePath("appsettings.json"), optional: false, reloadOnChange: true)
            .AddJsonFile(state.RetrieveInternalFilePath("localsettings.json"), optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }
    
    private static (List<ICollector>, List<ICollector>) SetupCollectors(AppConfig appConfig, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        var npcCollectorService = new CollectorConfigFactory(appConfig.RetrieveCollectorNpcConfigFilePath(state));
        var containerCollectorService = new CollectorConfigFactory(appConfig.RetrieveCollectorContainerConfigPath(state));

        var npcCollectors = Enum.GetValues(typeof(CollectorType))
            .Cast<CollectorType>()
            .Select(npcCollectorService.CreateCollector)
            .ToList();

        var containerCollectors = Enum.GetValues(typeof(CollectorType))
            .Cast<CollectorType>()
            .Select(containerCollectorService.CreateCollector)
            .ToList();

        return (npcCollectors, containerCollectors);
    }
}