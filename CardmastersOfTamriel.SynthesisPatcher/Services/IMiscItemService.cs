using Mutagen.Bethesda.Skyrim;
using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.SynthesisPatcher;

public interface IMiscItemService
{
    MiscItem InsertAsMiscItem(Card card);
}