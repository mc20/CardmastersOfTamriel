namespace CardmastersOfTamriel.ImageProcessor;

public class Config
{
    public required GeneralSettings General { get; init; }
    public required PathSettings Paths { get; init; }
    public required ImageProperties ImageProperties { get; init; }
}

public class GeneralSettings
{
    public required double ImageSelectionPercentageForSet { get; set; }
    public required HashSet<string> DefaultMiscItemKeywords { get; set; }
}

public class PathSettings
{
    public const string DefaultFilenameForSeriesMetadataJson = "series_metadata.json";
    public const string DefaultFilenameForSetMetadataJson = "set_metadata.json";
    public const string DefaultFilenameForCardsJsonl = "cards.jsonl";
    public const string DefaultFilenameForCardsJsonlBackup = "cards.jsonl.backup";

    public required string SourceImagesFolderPath { get; set; }
    public required string OutputFolderPath { get; set; }
    public required string MasterMetadataFilePath { get; set; }
    public required string SetMetadataOverrideFilePath { get; set; }
    public required string RebuildListFilePath { get; set; }
    public required CardTierFilePaths TemplateFiles { get; set; }
}

public class CardTierFilePaths
{
    public required CardShapeFilePaths Tier1 { get; set; }
    public required CardShapeFilePaths Tier2 { get; set; }
    public required CardShapeFilePaths Tier3 { get; set; }
    public required CardShapeFilePaths Tier4 { get; set; }
}

public class CardShapeFilePaths
{
    public required string Portrait { get; set; }
    public required string Landscape { get; set; }
    public required string Square { get; set; }
}

public class ImageProperties
{
    public required int MaximumTextureHeight { get; set; }
    public required TargetSizes TargetSizes { get; set; }
    public required Offset Offset { get; set; }
}

public class TargetSizes
{
    public required Size Portrait { get; set; }
    public required Size Landscape { get; set; }
    public required Size Square { get; set; }
}

public class Size
{
    public required int Width { get; set; }
    public required int Height { get; set; }

    public SixLabors.ImageSharp.Size ToImageSharpSize()
    {
        return new SixLabors.ImageSharp.Size(Width, Height);
    }
}

public class Offset
{
    public required int X { get; set; }
    public required int Y { get; set; }

    public SixLabors.ImageSharp.Point ToImageSharpPoint()
    {
        return new SixLabors.ImageSharp.Point(X, Y);
    }
}