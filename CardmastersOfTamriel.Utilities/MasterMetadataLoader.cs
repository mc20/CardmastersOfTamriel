using System.Text.Json;
using System.Text.Json.Serialization;
using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.Utilities;

public class MasterMetadataLoader : IMasterMetadataLoader
{
    private readonly string _metadataJsonFilePath;

    public MasterMetadataLoader(string metadataJsonFilePath)
    {
        _metadataJsonFilePath = metadataJsonFilePath;
    }

    public MasterMetadata GetMasterMetadata()
    {
        if (!File.Exists(_metadataJsonFilePath))
        {
            return new MasterMetadata();
        }

        try
        {
            var jsonString =  File.ReadAllText(_metadataJsonFilePath);
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
            Logger.LogException(jsonEx, $"Failed to deserialize JSON from {_metadataJsonFilePath}");
            return new MasterMetadata();
        }
        catch (IOException ioEx)
        {
            Logger.LogException(ioEx, $"Failed to read file {_metadataJsonFilePath}");
            return new MasterMetadata();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Unexpected error occurred while loading data from {_metadataJsonFilePath}");
            return new MasterMetadata();
        }
    }
}
