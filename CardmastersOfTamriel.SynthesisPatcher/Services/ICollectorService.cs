using CardmastersOfTamriel.SynthesisPatcher.Models;

namespace CardmastersOfTamriel.SynthesisPatcher;

public interface ICollectorService
{
    ICollector CreateCollector(CollectorType type);
}
