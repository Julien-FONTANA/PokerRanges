using PokerRanges.Core.Table;

namespace PokerRanges.Core.Preflop;

public sealed record PreflopSituation
{
    public required PreflopContext Context { get; init; }

    public FacingRelation? Relation { get; init; }

    public Position? Aggressor { get; init; }

    public required int PlayersLeftToAct { get; init; }

    public required double DepthInBigBlinds { get; init; }

    public required double AmountToCallInBigBlinds { get; init; }

    public required double PotInBigBlinds { get; init; }

    public required int Limpers { get; init; }

    public ChartKey ToKey()
    {
        return new ChartKey(Context, PlayersLeftToAct, Relation, DepthInBigBlinds);
    }
}
