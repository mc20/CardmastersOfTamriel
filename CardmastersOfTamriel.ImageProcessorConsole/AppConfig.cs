namespace CardmastersOfTamriel.ImageProcessorConsole;

public class AppConfig
{
    public required string SourceFolderPath { get; set; }
    public required string OutputFolderPath { get; set; }
    public required string MasterMetadataPath { get; set; }
    public required string TemplateFilePathPortrait { get; set; }
    public required string TemplateFilePathLandscape { get; set; }
    public required string TemplateFilePathSquare { get; set; }
}
