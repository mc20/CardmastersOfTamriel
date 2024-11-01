using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.SynthesisPatcher.Services;

public interface IMasterMetadataLoader
{
    Task<MasterMetadata> GetMasterMetadataAsync();
}