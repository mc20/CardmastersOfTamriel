using System.Runtime.CompilerServices;
using System.Text.Json;
using CardmastersOfTamriel.Models;
using Serilog;

namespace CardmastersOfTamriel.Utilities;

public class MasterMetadataHandler
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

    /// <summary>
    /// Creates a backup of the metadata file with a timestamp in the filename.
    /// </summary>
    /// <returns>
    /// The file path of the created backup file, or an empty string if the backup creation failed.
    /// </returns>
    /// <remarks>
    /// The backup file is saved in a "Backups" subdirectory within the directory of the original metadata file.
    /// If the backup file already exists, it will be deleted before creating a new one.
    /// </remarks>
    public string CreateBackup()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var directoryPath = Path.GetDirectoryName(_metadataFilePath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            Log.Error($"Failed to create backup for metadata file: '{_metadataFilePath}' because the directory path is empty.");
            return string.Empty;
        }

        var backupDirectoryPath = Path.Combine(directoryPath, "Backups");
        if (!Directory.Exists(backupDirectoryPath))
        {
            Directory.CreateDirectory(backupDirectoryPath);
        }

        var backupFilePath = Path.Combine(backupDirectoryPath, $"master_metadata_{timestamp}.backup");

        if (File.Exists(_metadataFilePath))
        {
            File.Copy(_metadataFilePath, backupFilePath);
        }
        else
        {
            Log.Information($"No existing metadata file found at '{_metadataFilePath}'. Created backup directory at '{backupDirectoryPath}'.");
        }

        return backupFilePath;
    }

    public void WriteMetadataToFile([CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)

    {
        var serializedJson = JsonSerializer.Serialize(Metadata, JsonSettings.Options);
        File.WriteAllText(_metadataFilePath, serializedJson);
        Log.Verbose(
            $"SAVING METADATA: {Path.GetFileName(callerFilePath)} Caller '{callerName}' (line:{callerLineNumber}) to '{_metadataFilePath}'");
    }
}