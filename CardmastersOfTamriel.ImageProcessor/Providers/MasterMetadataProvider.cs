using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessor.Providers;

public class MasterMetadataProvider
{
    private static readonly Lazy<Task<MasterMetadataProvider>> MetadataProviderInstanceAsync = new(() => CreateAsync(CancellationToken.None));

    public MasterMetadataHandler MetadataHandler { get; }

    private MasterMetadataProvider(MasterMetadataHandler metadataHandler)
    {
        MetadataHandler = metadataHandler;
    }

    public static Task<MasterMetadataProvider> InstanceAsync(CancellationToken cancellationToken = default) =>
        MetadataProviderInstanceAsync.Value;

    private static async Task<MasterMetadataProvider> CreateAsync(CancellationToken cancellationToken)
    {
        var config = ConfigurationProvider.Instance.Config;
        var metadataHandler = new MasterMetadataHandler(config.Paths.MasterMetadataFilePath);
        await metadataHandler.LoadFromFileAsync(cancellationToken); // Asynchronous call
        return new MasterMetadataProvider(metadataHandler);
    }
}
