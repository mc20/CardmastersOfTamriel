namespace CardmastersOfTamriel.Models;

public class MasterMetadata
{
    public HashSet<CardSeries>? Series { get; set; }

    public MasterMetadata()
    {
        Series = [];
    }
}
