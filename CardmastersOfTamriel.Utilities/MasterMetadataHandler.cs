using System.Text.Json;
using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.Utilities;

public class MasterMetadataHandler
{
    private static MasterMetadataHandler? instance = null;
    private static readonly object lockObject = new object();
    public MasterMetadata Metadata { get; private set; }
    private string _masterMetadataPath { get; set; }


    private MasterMetadataHandler(string masterMetadataPath)
    {
        Metadata = new MasterMetadata();
        _masterMetadataPath = masterMetadataPath;
    }

    public static MasterMetadataHandler CreateInstance(string masterMetadataPath)
    {
        lock (lockObject)
        {
            instance ??= new MasterMetadataHandler(masterMetadataPath);
            return instance;
        }
    }

    // Property to get the Singleton instance
    public static MasterMetadataHandler Instance
    {
        get
        {
            if (instance == null)
            {
                throw new InvalidOperationException("MasterMetadataManager has not been initialized. Call CreateInstance() first.");
            }
            return instance;
        }
    }

    public void LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Metadata.Series = [];
            return;
        }

        try
        {
            var jsonString = File.ReadAllText(filePath);
            var metadata = JsonSerializer.Deserialize<MasterMetadataHandler>(jsonString, JsonSettings.Options);
            Metadata.Series = [];
        }
        catch (JsonException jsonEx)
        {
            Logger.LogException(jsonEx, $"Failed to deserialize JSON from {filePath}");
            Metadata.Series = [];
        }
        catch (IOException ioEx)
        {
            Logger.LogException(ioEx, $"Failed to read file {filePath}");
            Metadata.Series = [];
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Unexpected error occurred while loading data from {filePath}");
            Metadata.Series = [];
        }
    }

    public void WriteMetadataToFile()
    {
        var serializedJson = JsonSerializer.Serialize(Metadata, JsonSettings.Options);
        var jsonFilePath = Path.Combine(_masterMetadataPath, "master_metadata.json");
        File.WriteAllText(jsonFilePath, serializedJson);
        Logger.LogAction($"Serialized JSON written to file: '{jsonFilePath}'", LogMessageType.Verbose);
    }
}
