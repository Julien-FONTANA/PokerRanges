using PokerRanges.Core.Preflop;

namespace PokerRanges.Data;

public sealed record ChartDocument
{
    public string Source { get; init; } = string.Empty;

    public IReadOnlyList<ChartEntry> Charts { get; init; } = [];
}

public sealed record ChartEntry
{
    public PreflopContext Context { get; init; }

    public int PlayersLeftToAct { get; init; }

    public FacingRelation? Relation { get; init; }

    public double DepthInBigBlinds { get; init; }

    public IReadOnlyList<ChartActionEntry> Actions { get; init; } = [];
}

public sealed record ChartActionEntry
{
    public ChartActionKind Kind { get; init; }

    public double SizeInBigBlinds { get; init; }

    public string Range { get; init; } = string.Empty;
}
