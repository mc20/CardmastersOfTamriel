using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Distribution.Configuration;

public class DistributionConfiguration
{
    public required string TargetName { get; set; }
    public required string DistributionFilePath { get; set; }
    public required string CollectorConfigFilePath { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(TargetName))
            throw new InvalidOperationException("TargetName must be specified");

        if (string.IsNullOrWhiteSpace(DistributionFilePath))
            throw new InvalidOperationException($"DistributionFilePath must be specified for target {TargetName}");

        if (string.IsNullOrWhiteSpace(CollectorConfigFilePath))
            throw new InvalidOperationException($"CollectorConfigPath must be specified for target {TargetName}");
    }

    public bool ValidateFilePaths()
    {
        if (!File.Exists(DistributionFilePath))
        {
            Log.Error($"Distribution file not found for {TargetName} at: {DistributionFilePath}");
            return false;
        }

        if (!File.Exists(CollectorConfigFilePath))
        {
            Log.Error($"Collector config file not found for {TargetName} at: {CollectorConfigFilePath}");
            return false;
        }

        return true;
    }
}
