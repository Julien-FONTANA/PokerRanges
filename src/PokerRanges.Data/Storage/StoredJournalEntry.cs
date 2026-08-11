namespace PokerRanges.Data.Storage;

public sealed class StoredJournalEntry
{
    public DateTimeOffset PlayedAt { get; set; }

    public StoredHand Hand { get; set; } = new();

    public string Advice { get; set; } = string.Empty;

    public List<string> Rationale { get; set; } = [];
}
