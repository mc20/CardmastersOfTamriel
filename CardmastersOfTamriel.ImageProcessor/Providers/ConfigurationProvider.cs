using Microsoft.Extensions.Configuration;

namespace CardmastersOfTamriel.ImageProcessor.Providers;

public class ConfigurationProvider
{
    private static readonly Lazy<ConfigurationProvider> ConfigProviderInstance = new(() => new ConfigurationProvider());
    public Config Config { get; }

    private ConfigurationProvider()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("localsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        Config = configuration.Get<Config>() ?? throw new InvalidOperationException("Failed to load configuration.");
    }

    public static ConfigurationProvider Instance => ConfigProviderInstance.Value;
}