using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Models;

namespace CardmastersOfTamriel.SynthesisPatcher.Configuration;

public interface ICollectorConfigFactory
{
    ICollector? CreateCollector(CollectorType type);
}
