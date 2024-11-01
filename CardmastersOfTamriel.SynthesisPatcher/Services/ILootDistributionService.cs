using CardmastersOfTamriel.SynthesisPatcher.Models;

namespace CardmastersOfTamriel.SynthesisPatcher;

public interface ILootDistributionService
{
    void DistributeToCollector(ICollector collector);
}
