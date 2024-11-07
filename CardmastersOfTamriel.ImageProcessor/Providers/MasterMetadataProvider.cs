using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessor.Providers;

public class MasterMetadataProvider
{
    private static readonly Lazy<MasterMetadataProvider> MetadataProviderInstance = new(() => new MasterMetadataProvider());
    public MasterMetadataHandler MetadataHandler { get; }

    private MasterMetadataProvider()
    {
        var config = ConfigurationProvider.Instance.Config;
        MetadataHandler = new MasterMetadataHandler(config.Paths.MasterMetadataFilePath);
        MetadataHandler.LoadFromFile();
    }

    public static MasterMetadataProvider Instance => MetadataProviderInstance.Value;
}