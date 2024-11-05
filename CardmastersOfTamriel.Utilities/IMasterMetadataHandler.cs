using System.Runtime.CompilerServices;
using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.Utilities;

public interface IMasterMetadataHandler
{
    MasterMetadata Metadata { get; }
    void InitializeEmptyMetadata();
    void LoadFromFile();
    void WriteMetadataToFile([CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0);
}
