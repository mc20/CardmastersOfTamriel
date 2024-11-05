using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.ImageProcessorConsole.Processors;

public struct ConversionResult
{
    public ConversionResult()
    {
    }

    public CardShape? Shape { get; set; } = null;
    public string DestinationAbsoluteFilePath { get; set; } = string.Empty;
}