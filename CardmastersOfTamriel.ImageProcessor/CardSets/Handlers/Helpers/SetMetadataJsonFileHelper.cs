using CardmastersOfTamriel.ImageProcessor.Configuration;
using CardmastersOfTamriel.Models;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Helpers;

public static class SetMetadataJsonFileHelper
{
    public static void BackupExistingSetMetadataFile(CardSet set)
    {
        var jsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForSetMetadataJson);
        var jsonBackupFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForSetMetadataJsonBackup);

        if (!File.Exists(jsonFilePath)) return;

        File.Move(jsonFilePath, jsonBackupFilePath, true);
        File.Delete(jsonFilePath);
        Log.Information("Backed up existing CardSet metadata file to '{jsonBackupFilePath}'", jsonBackupFilePath);
    }
}