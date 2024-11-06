using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Models;

namespace CardmastersOfTamriel.SynthesisPatcher.Services;

public interface ICollectorFactory
{
    ICollector CreateCollector(CollectorType type);
}
