using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Collectors.Targets;
using CardmastersOfTamriel.SynthesisPatcher.Configuration;
using CardmastersOfTamriel.SynthesisPatcher.Models;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Collectors.Services;

public class CollectorDistributionService
{
    private readonly Dictionary<CollectorType, IDistributionStrategy> _strategies;

    public CollectorDistributionService(Dictionary<CollectorType, IDistributionStrategy> strategies)
    {
        _strategies = strategies;
    }

    // public void DistributeToAll(DistributionSettings settings,
    //     Func<ICollectorConfig, IFormLink<IItemGetter>> leveledItemSelector)
    // {
    //     foreach (var config in settings.Configurations)
    //     {
    //         var collectors = new CollectorConfigFactory(config.CollectorConfigPath).LoadCollectors();
    //
    //         if (!_strategies.TryGetValue(config.Type, out var strategy))
    //         {
    //             Log.Error($"No strategy found for collector type: {config.Type}");
    //             continue;
    //         }
    //         
    //         foreach(var collector)
    //     }
    // }
}

public interface IDistributionStrategy
{
}