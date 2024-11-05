using CardmastersOfTamriel.SynthesisPatcher.Models;

namespace CardmastersOfTamriel.SynthesisPatcher;

public interface ICollectorFactory
{
    ICollector CreateCollector(CollectorType type);
}
