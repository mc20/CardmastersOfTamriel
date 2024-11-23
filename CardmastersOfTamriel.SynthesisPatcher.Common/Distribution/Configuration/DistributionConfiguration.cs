using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Common.Distribution.Configuration;

public class DistributionConfiguration
{
    public required string TargetName { get; set; }
    public required string DistributionFilePath { get; set; }
    public required HashSet<string> CollectorConfigFilePaths { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(TargetName))
            throw new InvalidOperationException("TargetName must be specified");

        if (string.IsNullOrWhiteSpace(DistributionFilePath))
            throw new InvalidOperationException($"DistributionFilePath must be specified for target {TargetName}");

        if (CollectorConfigFilePaths == null || CollectorConfigFilePaths.Count == 0)
            throw new InvalidOperationException($"CollectorConfigFilePaths must be specified for target {TargetName}");
    }

    public bool ValidateFilePaths()
    {
        if (!File.Exists(DistributionFilePath))
        {
            Log.Error($"Distribution file not found for {TargetName} at: {DistributionFilePath}");
            return false;
        }

        foreach (var configFilePath in CollectorConfigFilePaths)
        {
            if (!File.Exists(configFilePath))
            {
                Log.Error($"Collector config file not found for {TargetName} at: {configFilePath}");
                return false;
            }
        }

        return true;
    }
}
