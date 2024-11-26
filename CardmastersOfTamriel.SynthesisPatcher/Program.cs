using CardmastersOfTamriel.SynthesisPatcher.LeveledItems;
using CardmastersOfTamriel.SynthesisPatcher.MiscItems.Services;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Microsoft.Extensions.Configuration;
using Mutagen.Bethesda.Plugins;
using Serilog;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using CardmastersOfTamriel.SynthesisPatcher.Distribution;
using System.Text.Json;
using CardmastersOfTamriel.SynthesisPatcher.Common.Configuration;

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

    private static IConfigurationRoot SetupConfiguration(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        return new ConfigurationBuilder()
            .AddJsonFile(state.RetrieveInternalFile("appsettings.json"), optional: false, reloadOnChange: true)
            .AddJsonFile(state.RetrieveInternalFile("localsettings.json"), optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static async Task RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        var customMod = FormKeyGeneratorProvider.Instance.SkyrimMod;
        // customMod.ModHeader.Flags |= SkyrimModHeader.HeaderFlag.Small; // Set for ESL flag

        var configuration = SetupConfiguration(state);

        var patcherConfig = configuration.Get<PatcherConfiguration>();
        if (patcherConfig is null)
        {
            Console.WriteLine("App config is missing");
            return;
        }

        var patcherConfigJson = JsonSerializer.Serialize(patcherConfig, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine("Patcher Configuration:");
        Console.WriteLine(patcherConfigJson);

        patcherConfig.ApplyInternalFilePaths(state);

        if (string.IsNullOrEmpty(patcherConfig.LogOutputFilePath))
        {
            Console.WriteLine("Log output file path is missing");
            return;
        }

        SetupLogging(patcherConfig);

        patcherConfig.Validate();
        if (!patcherConfig.ValidateFilePaths())
        {
            Log.Fatal("File paths are invalid");

            Log.Debug($"MasterMetadataFilePath: {patcherConfig.MasterMetadataFilePath}");
            Log.Debug($"LogOutputFilePath: {patcherConfig.LogOutputFilePath}");
            Log.Debug($"DistributionConfigurations Count: {patcherConfig.DistributionConfigurations?.Count ?? 0}");
            Log.Debug(JsonSerializer.Serialize(patcherConfig, new JsonSerializerOptions() { WriteIndented = true }));

            return;
        }

        var cancellationSource = new CancellationTokenSource();

        KeywordHelper.AddStandardKeywords(customMod);
        await KeywordHelper.AddUniqueSeriesNamesAsKeywordsAsync(patcherConfig, state, customMod, cancellationSource.Token);

        // Creating card to leveled item mapping
        Log.Information("Creating card to leveled item mapping..");
        var cardService = new CardLeveledItemService(patcherConfig, state, customMod);
        var cardTierToLeveledItemMapping = await cardService.CreateCardTierToLeveledItemMappingAsync(cancellationSource.Token);

        var mappingService = new CollectorProbabilityMappingService(customMod, cardTierToLeveledItemMapping);

        var distributor = new CardCollectionDistributor(state, customMod, mappingService, patcherConfig, cancellationSource.Token);
        await distributor.DistributeToCollectorsInWorldAsync<LeveledItem>();
        await distributor.DistributeToCollectorsInWorldAsync<Container>();
        await distributor.DistributeToCollectorsInWorldAsync<Npc>();

        ValidateBeforeWrite(customMod);

        using var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
        var desiredFilePath = Path.Combine(env.DataFolderPath, "CardmastersOfTamriel.esp");

        customMod.WriteToBinary(
            desiredFilePath,
            new BinaryWriteParameters() { MastersListOrdering = new MastersListOrderingByLoadOrder(state.LoadOrder) });

        Log.Information("Mod successfully created and written to disk.");

        await Log.CloseAndFlushAsync();
    }

    private static void SetupLogging(PatcherConfiguration appConfig)
    {
        Log.Information("Setting up logging.. saving to {0}", appConfig.LogOutputFilePath);

        // var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        File.Delete(appConfig.LogOutputFilePath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(appConfig.LogOutputFilePath)
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();
    }

    private static void ValidateBeforeWrite(ISkyrimMod mod)
    {
        Log.Information("Validating mod before writing..");

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

        if (duplicates.Count == 0) return;
        
        Log.Error("Duplicate FormKeys found:");
        foreach (var dup in duplicates)
        {
            Log.Error($"Group: {dup.GroupName}, FormKey: {dup.FormKey}");
        }

        throw new Exception($"Found {duplicates.Count} duplicate FormKeys");
    }
}
