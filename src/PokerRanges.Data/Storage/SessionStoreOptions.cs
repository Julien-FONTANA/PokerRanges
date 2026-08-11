namespace PokerRanges.Data.Storage;

public sealed record SessionStoreOptions
{
    public required string PreferencesFilePath { get; init; }

    public required string HandFilePath { get; init; }

    public required string JournalFilePath { get; init; }

    /// <summary>
    /// Beyond this, the oldest hands drop out of the journal. A log nobody ever reads back because
    /// it is too long is useless; a hundred hands covers a playing session comfortably.
    /// </summary>
    public int JournalCapacity { get; init; } = 100;
}
