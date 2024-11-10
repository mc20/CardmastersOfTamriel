using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessor.Models;

public struct ConversionResult
{
    public ConversionResult()
    {
    }

    public CardShape? Shape { get; init; } = null;
    public string? DestinationAbsoluteFilePath { get; init; } = string.Empty;
}