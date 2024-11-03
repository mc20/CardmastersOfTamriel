using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public class ImageProcessor
{
    private readonly AppConfig _appConfig;
    private readonly ImageHelper _imageHelper;
    private readonly CardTierProcessor _tierProcessor;

    public ImageProcessor(AppConfig appConfig)
    {
        _appConfig = appConfig;
        _imageHelper = new ImageHelper(appConfig);
        _tierProcessor = new CardTierProcessor(_appConfig, _imageHelper);
    }

    public void Start()
    {
        FileOperations.EnsureDirectoryExists(_appConfig.OutputFolderPath);

        foreach (var tierSourceFolderPath in Directory.EnumerateDirectories(_appConfig.SourceFolderPath))
        {
            Logger.LogAction($"Tier Source Folder Path: '{tierSourceFolderPath}'");

            var tierDestinationFolderPath = Path.Combine(_appConfig.OutputFolderPath, Path.GetFileName(tierSourceFolderPath));
            FileOperations.EnsureDirectoryExists(tierDestinationFolderPath);

            _tierProcessor.ProcessTierFolder(tierSourceFolderPath, tierDestinationFolderPath);
        }
    }
}