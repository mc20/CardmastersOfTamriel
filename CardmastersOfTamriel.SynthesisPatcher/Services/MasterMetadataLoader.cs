using System.Text.Json;
using System.Text.Json.Serialization;
using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.SynthesisPatcher.Services;

public class MasterMetadataLoader : IMasterMetadataLoader
{
    private readonly string _metadataJsonFilePath;

    public MasterMetadataLoader(string metadataJsonFilePath)
    {
        _metadataJsonFilePath = metadataJsonFilePath;
    }

    public async Task<MasterMetadata> GetMasterMetadataAsync()
    {
        if (!File.Exists(_metadataJsonFilePath))
        {
            return new MasterMetadata();
        }

        try
        {
            var jsonString = await File.ReadAllTextAsync(_metadataJsonFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var metadata = JsonSerializer.Deserialize<MasterMetadata>(jsonString, options);
            return metadata ?? new MasterMetadata();
        }
        catch (JsonException jsonEx)
        {
            DebugTools.LogException(jsonEx, $"Failed to deserialize JSON from {_metadataJsonFilePath}");
            return new MasterMetadata();
        }
        catch (IOException ioEx)
        {
            DebugTools.LogException(ioEx, $"Failed to read file {_metadataJsonFilePath}");
            return new MasterMetadata();
        }
        catch (Exception ex)
        {
            DebugTools.LogException(ex, $"Unexpected error occurred while loading data from {_metadataJsonFilePath}");
            return new MasterMetadata();
        }
    }
}
