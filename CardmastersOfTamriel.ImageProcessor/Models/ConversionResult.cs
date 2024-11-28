using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessor.Models;

public struct ConversionResult
{
    public ConversionResult()
    {
    }

    public required CardShape Shape { get; init; }
    public required string DestinationAbsoluteFilePath { get; init; }
}