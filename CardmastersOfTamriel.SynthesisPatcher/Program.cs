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
        var customMod = FormKeyGeneratorProvider.Instance.SkyrimMod;
        // customMod.ModHeader.Flags |= SkyrimModHeader.HeaderFlag.Small; // Set for ESL flag

        var configuration = SetupConfiguration(state);

        var appConfig = configuration.Get<AppConfig>();
        if (appConfig is null)
        {
            Log.Error("App config is missing");
            return;
        }

        appConfig.ApplyInternalFilePaths(state);

        if (string.IsNullOrEmpty(appConfig.LogOutputFilePath))
        {
            Log.Error("Log output file path is missing");
            return;
        }

        SetupLogging(appConfig);

        Log.Information("Loading metadata..");
        var metadataHandler = new MasterMetadataHandler(appConfig.MasterMetadataFilePath);
        metadataHandler.LoadFromFile();

        if (metadataHandler.Metadata.Series?.Count == 0)
        {
            Log.Error("Metadata is missing");
            return;
        }

        // Log.Information("Creating custom Keywords..");
        // _ = customMod.Keywords.AddNewWithId("CMT_CollectorCard");

        // Creating card to leveled item mapping
        var cardTierToLeveledItemMapping = CreateCardTierToLeveledItemMapping(metadataHandler, customMod, state);
        var probabilityMapper = new CollectorProbabilityMapper(customMod, cardTierToLeveledItemMapping);

        SetupLeveledItems(state, appConfig, probabilityMapper, customMod);
        SetupContainers(state, appConfig, probabilityMapper, customMod);

        ValidateBeforeWrite(customMod);

        using var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
        var desiredFilePath = Path.Combine(env.DataFolderPath, "CardmastersOfTamriel.esp");

        customMod.WriteToBinary(
            desiredFilePath,
            new BinaryWriteParameters() { MastersListOrdering = new MastersListOrderingByLoadOrder(state.LoadOrder) });

        Log.Information("Mod successfully created and written to disk.");

        await Log.CloseAndFlushAsync();
    }

    public static void ValidateBeforeWrite(ISkyrimMod mod)
    {
        var formKeys = new HashSet<FormKey>();
        var duplicates = new List<(string GroupName, string FormKey)>();

        // Check all record groups
        foreach (var record in mod.EnumerateMajorRecords())
        {
            if (!formKeys.Add(record.FormKey))
            {
                duplicates.Add((record.GetType().Name, $"{record.FormKey}|{record.EditorID}"));
            }
        }

        if (duplicates.Count != 0)
        {
            Log.Error("Duplicate FormKeys found:");
            foreach (var dup in duplicates)
            {
                Log.Error($"Group: {dup.GroupName}, FormKey: {dup.FormKey}");
            }

            throw new Exception($"Found {duplicates.Count} duplicate FormKeys");
        }
    }

    private static void SetupLogging(AppConfig appConfig)
    {
        Log.Information("Setting up logging.. saving to {0}", appConfig.LogOutputFilePath);

        // var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        File.Delete(appConfig.LogOutputFilePath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(appConfig.LogOutputFilePath)
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();
    }

    private static IConfigurationRoot SetupConfiguration(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        return new ConfigurationBuilder()
            .AddJsonFile(state.RetrieveInternalFile("appsettings.json"), optional: false, reloadOnChange: true)
            .AddJsonFile(state.RetrieveInternalFile("localsettings.json"), optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    /// <summary>
    /// Creates a mapping of CardTier to LeveledItem.
    /// </summary>
    /// <param name="metadataHandler">The handler for master metadata.</param>
    /// <param name="customMod">The custom Skyrim mod instance.</param>
    /// <param name="state">The patcher state containing Skyrim mod data.</param>
    /// <returns>A dictionary mapping each CardTier to its corresponding LeveledItem.</returns>
    private static Dictionary<CardTier, LeveledItem> CreateCardTierToLeveledItemMapping(
        MasterMetadataHandler metadataHandler, ISkyrimMod customMod, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        Log.Information("Creating CardTier to LeveledItem mapping..");

        var helper = new MetadataHelper(metadataHandler);
        var cardList = helper.GetCards().ToHashSet();

        var miscService = new CardMiscItemCreator(state, customMod);
        var mappedMiscItems = miscService.InsertAndMapCardsToMiscItems(cardList);

        // Get all cards grouped by CardTier
        var cardTierItemCreator = new TieredCardLeveledItemAssembler(state, customMod);
        return cardTierItemCreator.CreateCardTierLeveledItems(mappedMiscItems);
    }

    private static void SetupLeveledItems(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, AppConfig appConfig,
        CollectorProbabilityMapper probabilityLeveledListBuilder, ISkyrimMod customMod)
    {
        Log.Information("Setting up entries for LeveledItems..");

        var configuration = CollectorConfigFactory.RetrieveCollectorConfiguration(appConfig.DistributionsFilePaths.LeveledItem);
        Log.Verbose($"Loaded Configuration for: {configuration.Category} from '{appConfig.DistributionsFilePaths.LeveledItem}'");

        var collectorLeveledListMapping = probabilityLeveledListBuilder.CreateCollectorTypeMapping(configuration);
        Log.Verbose($"Mapped {collectorLeveledListMapping.Count} CollectorTypes with LeveledItems.");

        var collectorTypeMappings = CollectorLoader.GetCollectorIds(appConfig.CollectorsFilePaths.LeveledItem);
        foreach (var collectorTier in collectorTypeMappings)
        {
            Log.Verbose("Adding Entries for CollectorType '{0}'..", collectorTier.Key);

            foreach (var target in collectorTier.Value)
            {
                if (collectorLeveledListMapping.TryGetValue(collectorTier.Key, out var cardTierLeveledItem))
                {
                    Log.Verbose($"Adding LeveledItem '{cardTierLeveledItem.EditorID}' as Entry for LeveledItem '{target}'..");

                    var leveledItem = state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides().FirstOrDefault(l => l.EditorID == target);
                    if (leveledItem is null)
                    {
                        Log.Warning($"Target LeveledItem '{target}' not found in load order.");
                        continue;
                    }

                    var recordForModification = customMod.LeveledItems.GetOrAddAsOverride(leveledItem);
                    LeveledItemEntryBuilder.AddEntries(recordForModification, cardTierLeveledItem, 1, 1);
                }
                else
                {
                    Log.Warning($"CollectorType '{collectorTier.Key}' not found in mapping.");
                }
            }
        }
    }

    private static void SetupContainers(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, AppConfig appConfig,
        CollectorProbabilityMapper probabilityLeveledListBuilder, ISkyrimMod customMod)
    {
        Log.Information("Setting up items for Containers..");

        var configuration = CollectorConfigFactory.RetrieveCollectorConfiguration(appConfig.DistributionsFilePaths.Container);
        Log.Verbose(
            $"Loaded Configuration for: {configuration.Category} from '{appConfig.DistributionsFilePaths.LeveledItem}'");

        var collectorLeveledListMapping = probabilityLeveledListBuilder.CreateCollectorTypeMapping(configuration);
        Log.Verbose($"Mapped {collectorLeveledListMapping.Count} CollectorTypes with LeveledItems.");

        var collectorTypeMappings = CollectorLoader.GetCollectorIds(appConfig.CollectorsFilePaths.Container);
        foreach (var collectorTier in collectorTypeMappings)
        {
            Log.Verbose("Adding Items for CollectorType '{0}'..", collectorTier.Key);

            foreach (var target in collectorTier.Value)
            {
                if (collectorLeveledListMapping.TryGetValue(collectorTier.Key, out var cardTierLeveledItem))
                {
                    Log.Verbose($"Adding LeveledItem '{cardTierLeveledItem.EditorID}' as Entry for LeveledItem '{target}'..");

                    var container = state.LoadOrder.PriorityOrder.Container().WinningOverrides().FirstOrDefault(c => c.EditorID == target);
                    if (container is null)
                    {
                        Log.Warning($"Target LeveledItem '{target}' not found in load order.");
                        continue;
                    }


                    var recordForModification = customMod.Containers.GetOrAddAsOverride(container);
                    ContainerItemBuilder.AddEntries(recordForModification, cardTierLeveledItem, 1, 1);
                }
                else
                {
                    Log.Warning($"CollectorType '{collectorTier.Key}' not found in mapping.");
                }
            }
        }
    }
}