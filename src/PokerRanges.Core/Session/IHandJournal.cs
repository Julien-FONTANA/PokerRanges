namespace PokerRanges.Core.Session;

/// <summary>
/// Le carnet des mains jouées, de la plus récente à la plus ancienne. Borné : un journal qui
/// grossit sans fin finit par coûter plus à relire qu'il ne rapporte.
/// </summary>
public interface IHandJournal
{
    IReadOnlyList<JournalEntry> Entries { get; }

    void Append(JournalEntry entry);

    void Clear();
}
