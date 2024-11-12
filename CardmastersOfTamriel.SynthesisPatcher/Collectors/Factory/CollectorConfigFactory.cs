using CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Factory;

public static class CollectorConfigFactory
{

    /// <summary>
    /// Retrieves the collector configuration from the specified JSON file.
    /// </summary>
    /// <param name="configFilePath">The path to the JSON configuration file.</param>
    /// <returns>A <see cref="CollectorTypeConfiguration"/> object containing the configuration data.</returns>
    public static CollectorTypeConfiguration RetrieveCollectorConfiguration(string configFilePath) => JsonFileReader.ReadFromJson<CollectorTypeConfiguration>(configFilePath);
}
