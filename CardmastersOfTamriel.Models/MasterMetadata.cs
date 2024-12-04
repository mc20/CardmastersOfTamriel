namespace CardmastersOfTamriel.Models;

public class MasterMetadata
{
    public Dictionary<CardTier, HashSet<CardSeries>> Metadata { get; init; }
    
    public MasterMetadata()
    {
        Metadata = new Dictionary<CardTier, HashSet<CardSeries>>();
    }
}