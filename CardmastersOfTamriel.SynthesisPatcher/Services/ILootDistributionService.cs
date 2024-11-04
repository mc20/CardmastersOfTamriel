using CardmastersOfTamriel.SynthesisPatcher.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.SynthesisPatcher;

public interface ILootDistributionService
{
    void DistributeToCollector(ICollector collector, MasterMetadataHandler handler);
}
