using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Factory;
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
using CardmastersOfTamriel.SynthesisPatcher.Metadata;

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
        customMod.ModHeader.Flags |= SkyrimModHeader.HeaderFlag.Small;

        var configuration = SetupConfiguration(state);

        var appConfig = configuration.Get<AppConfig>();

        if (appConfig is null || string.IsNullOrEmpty(appConfig.LogOutputFilePath) ||
            string.IsNullOrEmpty(appConfig.MetadataFilePath))
        {
            Log.Error("App config is missing");
            return;
        }

        SetupLogging(appConfig, state);

        var metadataHandler = new MasterMetadataHandler(appConfig.RetrieveMetadataFilePath(state));
        metadataHandler.LoadFromFile();

        var formIdGenerator = new FormIdGenerator(customMod.ModKey);

        var helper = new MetadataHelper(metadataHandler);
        var allDistributableCards = helper.GetCards().ToHashSet();

        if (allDistributableCards.Count == 0)
        {
            Log.Error("No distributable cards found in metadata.");
            return;
        }

        Log.Verbose("Cards: {0}\n\n\t",
            string.Join("\n\t", allDistributableCards.Select(card => (card.Id, card.DisplayName)).ToList()));

        var keyword = customMod.Keywords.AddNew("CMOT_CollectorCard");

        var cardTierToLeveledItemMapping =
            CreateCardTierToLeveledItemMapping(metadataHandler, customMod, state, formIdGenerator);

        Log.Information(string.Empty);
        Log.Information("\n\nAssigning MiscItems to Collector LeveledItems..\n");

        var service = new CollectorLeveledItemService(customMod, formIdGenerator);
        
        var npcFactory = new CollectorConfigFactory(appConfig.RetrieveCollectorNpcConfigFilePath(state));
        var npcCollectors = npcFactory.LoadNpcCollectors();
        foreach (var collector in npcCollectors)
        {
            service.DistributeCardsToCollector(collector, cardTierToLeveledItemMapping);
        }
        
        var containerFactor = new CollectorConfigFactory(appConfig.RetrieveCollectorContainerConfigPath(state));
        var containerCollectors = containerFactor.LoadContainers();
        foreach (var collector in containerCollectors)
        {
            service.DistributeCardsToCollector(collector, cardTierToLeveledItemMapping);
        }

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
    
    private static Dictionary<CardTier, LeveledItem> CreateCardTierToLeveledItemMapping(
        MasterMetadataHandler metadataHandler, ISkyrimMod customMod, IPatcherState<ISkyrimMod, ISkyrimModGetter> state,
        FormIdGenerator formIdGenerator)
    {
        var helper = new MetadataHelper(metadataHandler);
        var cardList = helper.GetCards().ToHashSet();

        var miscService = new CardMiscItemCreator(state, customMod, formIdGenerator);
        var mappedMiscItems = miscService.InsertAndMapCardsToMiscItems(cardList);

        // Get all cards grouped by CardTier
        var cardTierItemCreator = new TieredCardLeveledItemAssembler(state, customMod);
        return cardTierItemCreator.CreateCardTierLeveledItems(mappedMiscItems);
    }
}