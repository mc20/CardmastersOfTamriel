using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Microsoft.Extensions.Configuration;
using CardmastersOfTamriel.SynthesisPatcher.Services;
using CardmastersOfTamriel.SynthesisPatcher.Models;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins.Binary.Parameters;

namespace CardmastersOfTamriel.SynthesisPatcher;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Load configuration from appsettings.json and other sources
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("localsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var filePathsConfig = configuration.Get<AppConfig>();

        if (filePathsConfig is null)
        {
            Console.Error.WriteLine("Failed to load configuration.");
            return 1;
        }

        return await SynthesisPipeline.Instance
            .AddPatch<ISkyrimMod, ISkyrimModGetter>(state => RunPatch(state, filePathsConfig))
            .SetTypicalOpen(GameRelease.SkyrimSE, "CardmastersOfTamriel.esp")
            .Run(args);
    }

    private static async Task RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, AppConfig appConfig)
    {
        var customMod = new SkyrimMod(new ModKey("CardmastersOfTamriel", ModType.Plugin), SkyrimRelease.SkyrimSE);

        var loader = new MasterMetadataLoader(appConfig.MasterMetadataPath);

        var distributors = new HashSet<ILootDistributorService>
        {
            new ContainerDistributorService(state, customMod, appConfig.ContainerConfigPath),
            new LeveledItemDistributor(state, customMod, appConfig.LeveledItemConfigPath)
        };

        var miscService = new MiscItemService(state, customMod);
        var metadata = await loader.GetMasterMetadataAsync();
        var lootService = new LootDistributionService(customMod, distributors, miscService, metadata);

        var collectorService = new CollectorService(appConfig.CollectorConfigPath);

        // Create collectors for each CollectorType
        var collectors = Enum.GetValues(typeof(CollectorType))
                             .Cast<CollectorType>()
                             .Select(type => collectorService.CreateCollector(type))
                             .ToList();

        // Distribute loot to each collector
        foreach (var collector in collectors)
        {
            lootService.DistributeToCollector(collector);
        }

        using var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
        var desiredFilePath = Path.Combine(env.DataFolderPath, "CardmastersOfTamriel.esp");

        customMod.WriteToBinary(
            desiredFilePath,
            new BinaryWriteParameters() { MastersListOrdering = new MastersListOrderingByLoadOrder(state.LoadOrder) });

        Console.WriteLine("Mod successfully created and written to disk.");
    }
}
