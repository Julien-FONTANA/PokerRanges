namespace PokerRanges.Core.Session;

/// <summary>
/// The log of hands played, most recent first. Bounded: a journal that grows without end ends up
/// costing more to read back than it returns.
/// </summary>
public interface IHandJournal
{
    IReadOnlyList<JournalEntry> Entries { get; }

    void Append(JournalEntry entry);

    void Clear();
}
