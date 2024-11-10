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
        customMod.ModHeader.Flags |= SkyrimModHeader.HeaderFlag.Small;

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

        var helper = new MetadataHelper(metadataHandler);
        var cardList = helper.GetCards().ToHashSet();

        if (cardList.Count == 0) return;

        Log.Verbose("Cards: {0}\n\n\t", string.Join("\n\t", cardList.Select(card => (card.Id, card.DisplayName)).ToList()));

        // var keyword = customMod.Keywords.AddNew("CMOT_CollectorCard");
        var keyword = customMod.Keywords.AddNew("CMOT_CollectorCard");
        
        var formIdGenerator = new FormIdGenerator(customMod.ModKey);
        var miscService = new CardMiscItemCreator(state, customMod, formIdGenerator);
        var mappedMiscItems = miscService.InsertAndMapCardsToMiscItems(cardList);

        var cardTierItemCreator = new TieredCardLeveledItemAssembler(state, customMod);
        var cardTierMappings = cardTierItemCreator.CreateCardTierLeveledItems(mappedMiscItems);

        Log.Information(string.Empty);
        Log.Information("\n\nAssigning MiscItems to Collector LeveledItems..\n");
        
        var npcFactory = new CollectorConfigFactory(appConfig.RetrieveCollectorNpcConfigFilePath(state));
        var collectors = Enum.GetValues(typeof(CollectorType))
            .Cast<CollectorType>()
            .Select(npcFactory.CreateCollector)
            .Where(collector => collector is not null)
            .ToList();

        // var target = new NpcTarget();
        // var service = new CollectorLeveledItemService(customMod, formIdGenerator, state);
        // foreach (var collector in collectors)
        // {
        //     service.DistributeCardsToCollector(collector, cardTierMappings, target);
        //     
        // }

        var npcDistributor = new NpcDistributor(appConfig, state, customMod);
        var npcItemProcessor = new CollectorLeveledItemDistributor(state, customMod, npcFactory, npcDistributor, formIdGenerator);
        npcItemProcessor.SetupCollectorLeveledEntries(cardTierMappings);

        var containerCollectorService = new CollectorConfigFactory(appConfig.RetrieveCollectorContainerConfigPath(state));
        var containerDistributor = new ContainerDistributor(appConfig, state, customMod);
        var containerItemProcessor = new CollectorLeveledItemDistributor(state, customMod, containerCollectorService, containerDistributor, formIdGenerator);
        containerItemProcessor.SetupCollectorLeveledEntries(cardTierMappings);

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
}