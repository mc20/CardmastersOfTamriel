using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.ImageProcessorConsole;

public class CardSetMirrorer
{
    private readonly CardSeries _series;

    public CardSetMirrorer(CardSeries series)
    {
        _series = series;
    }

    private void CreateMultipleFolders(string uniqueSetFolderName, List<string> sourceSetFolderPaths)
    {
        // Multiple folders: rename with incremented suffixes
        for (var index = 0; index < sourceSetFolderPaths.Count; index++)
        {
            var destinationSetFolderName = $"{uniqueSetFolderName}_{index + 1:D2}";
            var destinationSetFolderPath = Path.Combine(_series.DestinationFolderPath, destinationSetFolderName);

            SaveNewSetAndCreateAtDestination(destinationSetFolderName, destinationSetFolderPath);
        }
    }

    public void CreateSetsAtDestination(Dictionary<string, List<string>> groupedFolders)
    {
        foreach (var (setFolderName, sourceSetPaths) in groupedFolders)
        {
            if (sourceSetPaths.Count > 1)
            {
                CreateMultipleFolders(setFolderName, sourceSetPaths);
            }
            else
            {
                var destinationSetFolderPath = Path.Combine(_series.DestinationFolderPath, setFolderName);
                SaveNewSetAndCreateAtDestination(setFolderName, destinationSetFolderPath);
            }
        }
    }

    private void SaveNewSetAndCreateAtDestination(string newFolderName, string newFolderPath)
    {
        Directory.CreateDirectory(newFolderPath);

        var newSet = CardSetFactory.CreateNewSet(newFolderName);
        newSet.SeriesId = _series.Id;
        newSet.Tier = _series.Tier;
        newSet.SourceFolderPath = _series.SourceFolderPath;
        newSet.DestinationFolderPath = _series.DestinationFolderPath;

        _series.Sets ??= [];
        _series.Sets.Add(newSet);

        Logger.LogAction($"New Set: '{newSet.Id}' => Destination Folder Path: '{newSet.DestinationFolderPath}'");

        MasterMetadataHandler.Instance.WriteMetadataToFile();
    }
}