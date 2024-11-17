using CardmastersOfTamriel.SynthesisPatcher.Distribution.Configuration;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Configuration;

public class PatcherConfiguration
{
    public required string MasterMetadataFilePath { get; set; }
    public required string LogOutputFilePath { get; set; }
    public required HashSet<DistributionConfiguration> DistributionConfigurations { get; set; }

    public void ApplyInternalFilePaths(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {

        MasterMetadataFilePath = state.RetrieveInternalFile(MasterMetadataFilePath);
        LogOutputFilePath = state.RetrieveInternalFile(LogOutputFilePath);

        foreach (var config in DistributionConfigurations)
        {
            config.DistributionFilePath = state.RetrieveInternalFile(config.DistributionFilePath);
            config.CollectorConfigFilePaths = [.. config.CollectorConfigFilePaths.Select(state.RetrieveInternalFile)];
        }
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(MasterMetadataFilePath))
            throw new InvalidOperationException("MasterMetadataFilePath must be specified");

        if (string.IsNullOrWhiteSpace(LogOutputFilePath))
            throw new InvalidOperationException("LogOutputFilePath must be specified");

        if (!DistributionConfigurations.Any())
            throw new InvalidOperationException("At least one DistributionConfiguration must be specified");

        foreach (var config in DistributionConfigurations)
        {
            config.Validate();
        }

        // Check for duplicate target names
        var duplicateTargets = DistributionConfigurations
            .GroupBy(x => x.TargetName.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateTargets.Any())
        {
            throw new InvalidOperationException(
                $"Duplicate TargetNames found: {string.Join(", ", duplicateTargets)}");
        }
    }

    public DistributionConfiguration GetConfigurationForTarget(string targetName)
    {
        return DistributionConfigurations
            .FirstOrDefault(x => x.TargetName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"No configuration found for target: {targetName}");
    }

    public bool ValidateFilePaths()
    {
        if (!File.Exists(MasterMetadataFilePath))
        {
            Log.Error($"Master metadata file not found at: {MasterMetadataFilePath}");
            try
            {
                File.Create(MasterMetadataFilePath).Dispose();
                Log.Information($"Created missing master metadata file at: {MasterMetadataFilePath}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to create master metadata file at: {MasterMetadataFilePath}");
                return false;
            }
            return false;
        }

        // Log output path - ensure directory exists
        var logDirectory = Path.GetDirectoryName(LogOutputFilePath);
        if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
        {
            try
            {
                Directory.CreateDirectory(logDirectory);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to create log directory: {logDirectory}");
                return false;
            }
        }

        return DistributionConfigurations.All(config => config.ValidateFilePaths());
    }
}