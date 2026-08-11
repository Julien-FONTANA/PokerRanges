namespace PokerRanges.Data.Storage;

public sealed record SessionStoreOptions
{
    public required string PreferencesFilePath { get; init; }

    public required string HandFilePath { get; init; }

    public required string JournalFilePath { get; init; }

    /// <summary>
    /// Au-delà, les plus anciennes mains sortent du journal. Un carnet qu'on ne relit jamais parce
    /// qu'il est trop long ne sert à rien ; cent mains couvrent largement une session de jeu.
    /// </summary>
    public int JournalCapacity { get; init; } = 100;
}
