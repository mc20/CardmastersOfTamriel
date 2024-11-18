using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessor.Providers;

public class MasterMetadataProvider
{
    [Obsolete("Use InstanceAsync instead.", false)]
    private static readonly Lazy<MasterMetadataProvider> MetadataProviderInstance = new(CreateSync);
    private static readonly Lazy<Task<MasterMetadataProvider>> MetadataProviderInstanceAsync = new(() => CreateAsync(CancellationToken.None));

    public MasterMetadataHandler MetadataHandler { get; }

    private MasterMetadataProvider(MasterMetadataHandler metadataHandler)
    {
        MetadataHandler = metadataHandler;
    }

    [Obsolete("Use InstanceAsync instead.")]
    public static MasterMetadataProvider Instance => MetadataProviderInstance.Value;

    public static Task<MasterMetadataProvider> InstanceAsync(CancellationToken cancellationToken = default) =>
        MetadataProviderInstanceAsync.Value;

    [Obsolete("Use CreateAsync instead.", false)]
    private static MasterMetadataProvider CreateSync()
    {
        var config = ConfigurationProvider.Instance.Config;
        var metadataHandler = new MasterMetadataHandler(config.Paths.MasterMetadataFilePath);
        metadataHandler.LoadFromFile(); // Synchronous call
        return new MasterMetadataProvider(metadataHandler);
    }

    private static async Task<MasterMetadataProvider> CreateAsync(CancellationToken cancellationToken)
    {
        var config = ConfigurationProvider.Instance.Config;
        var metadataHandler = new MasterMetadataHandler(config.Paths.MasterMetadataFilePath);
        await metadataHandler.LoadFromFileAsync(cancellationToken); // Asynchronous call
        return new MasterMetadataProvider(metadataHandler);
    }
}
