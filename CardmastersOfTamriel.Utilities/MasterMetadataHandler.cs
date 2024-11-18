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

    [Obsolete("Use LoadFromFileAsync instead.", false)]
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
            throw;
        }
    }

    public async Task LoadFromFileAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_metadataFilePath))
        {
            Metadata.Series = [];
            return;
        }

        try
        {
            Metadata = await JsonFileReader.ReadFromJsonAsync<MasterMetadata>(_metadataFilePath, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to load metadata from {_metadataFilePath}");
            throw;
        }
    }

    [Obsolete("Use CreateBackupAsync instead.", false)]
    public string CreateBackup()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var directoryPath = Path.GetDirectoryName(_metadataFilePath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            Log.Error(
                $"Failed to create backup for metadata file: '{_metadataFilePath}' because the directory path is empty.");
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
            Log.Information(
                $"No existing metadata file found at '{_metadataFilePath}'. Created backup directory at '{backupDirectoryPath}'.");
        }

        return backupFilePath;
    }

    public async Task<string> CreateBackupAsync(CancellationToken cancellationToken)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var directoryPath = Path.GetDirectoryName(_metadataFilePath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            Log.Error(
                $"Failed to create backup for metadata file: '{_metadataFilePath}' because the directory path is empty.");
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
            await using var sourceStream = new FileStream(_metadataFilePath, FileMode.Open, FileAccess.Read);
            await using var destinationStream = new FileStream(backupFilePath, FileMode.Create, FileAccess.Write);
            await sourceStream.CopyToAsync(destinationStream, cancellationToken);
        }
        else
        {
            Log.Information(
                $"No existing metadata file found at '{_metadataFilePath}'. Created backup directory at '{backupDirectoryPath}'.");
        }

        return backupFilePath;
    }

    [Obsolete("Use WriteMetadataToFileAsync instead.", false)]
    public void WriteMetadataToFile([CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)

    {
        var serializedJson = JsonSerializer.Serialize(Metadata, JsonSettings.Options);
        File.WriteAllText(_metadataFilePath, serializedJson);
        Log.Debug(
            $"SAVING METADATA: {Path.GetFileName(callerFilePath)} Caller '{callerName}' (line:{callerLineNumber}) to '{_metadataFilePath}'");
    }

    public async Task WriteMetadataToFileAsync(CancellationToken cancellationToken,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)

    {
        await JsonFileWriter.WriteToJsonAsync(Metadata, _metadataFilePath, cancellationToken);
        Log.Debug(
            $"SAVING METADATA: {Path.GetFileName(callerFilePath)} Caller '{callerName}' (line:{callerLineNumber}) to '{_metadataFilePath}'");
    }
}