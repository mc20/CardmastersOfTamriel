namespace CardmastersOfTamriel.ImageProcessorConsole;

public class Config
{
    public required GeneralSettings General { get; set; }
    public required PathSettings Paths { get; set; }
    public required ImageProperties ImageProperties { get; set; }
}

public class GeneralSettings
{
    public required int MaxSampleSize { get; set; }
}

public class PathSettings
{
    public required string SourceImagesFolderPath { get; set; }
    public required string OutputFolderPath { get; set; }
    public required string MasterMetadataFilePath { get; set; }
    public required TemplateFiles TemplateFiles { get; set; }
}

public class TemplateFiles
{
    public required string Portrait { get; set; }
    public required string Landscape { get; set; }
    public required string Square { get; set; }
}

public class ImageProperties
{
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
