using System.Text.Json;

namespace CardmastersOfTamriel.SynthesisPatcher.Common.Configuration;

// Helper class to load the configuration
public static class ConfigurationLoader
{
    public static PatcherConfiguration LoadConfiguration(string configPath)
    {
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"Configuration file not found at: {configPath}");

        try
        {
            var jsonString = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<PatcherConfiguration>(jsonString)
                ?? throw new InvalidOperationException("Failed to deserialize configuration");

            config.Validate();

            if (!config.ValidateFilePaths())
            {
                throw new InvalidOperationException("One or more required files are missing");
            }

            return config;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid configuration file format: {ex.Message}", ex);
        }
    }
}