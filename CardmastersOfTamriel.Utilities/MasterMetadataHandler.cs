using System.Runtime.CompilerServices;
using System.Text.Json;
using CardmastersOfTamriel.Models;
using Serilog;

namespace CardmastersOfTamriel.Utilities;

public class MasterMetadataHandler : IMasterMetadataHandler
{
    private readonly string _metadataFilePath;
    public MasterMetadata Metadata { get; private set; }

    public MasterMetadataHandler(string metadataFilePath)
    {
        _metadataFilePath = metadataFilePath;
        Metadata = new MasterMetadata();
    }

    public void InitializeEmptyMetadata()
    {
        Metadata = new MasterMetadata();
    }

    public void LoadFromFile()
    {
        if (!File.Exists(_metadataFilePath))
        {
            Metadata.Series = [];
            return;
        }

        try
        {
            var jsonString = File.ReadAllText(_metadataFilePath);
            Metadata = JsonSerializer.Deserialize<MasterMetadata>(jsonString, JsonSettings.Options) ??
                       new MasterMetadata();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to load metadata from {_metadataFilePath}");
            Metadata.Series = [];
        }
    }

    public void WriteMetadataToFile([CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)

    {
        var serializedJson = JsonSerializer.Serialize(Metadata, JsonSettings.Options);
        File.WriteAllText(_metadataFilePath, serializedJson);
        Log.Information($"SAVING METADATA: {Path.GetFileName(callerFilePath)} Caller '{callerName}' (line:{callerLineNumber}) to '{_metadataFilePath}'");
    }
}