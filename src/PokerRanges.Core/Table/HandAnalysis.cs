using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Table;

public sealed record HandAnalysis
{
    public required Street Street { get; init; }

    public required double Pot { get; init; }

    public required double CurrentBet { get; init; }

    public required Position HeroPosition { get; init; }

    public required Position? NextToAct { get; init; }

    public required IReadOnlyList<Position> LivePositions { get; init; }

    public required IReadOnlyDictionary<Position, PotSnapshot> Snapshots { get; init; }

    public PotSnapshot Hero => Snapshots[HeroPosition];

    public bool IsHeroTurn => NextToAct == HeroPosition;

    public IReadOnlyList<Position> LiveOpponents => [.. LivePositions.Where(position => position != HeroPosition)];

    public PotSnapshot For(Position position)
    {
        if (!Snapshots.TryGetValue(position, out PotSnapshot? snapshot))
        {
            throw new TableException(TableText.NotSeated(PositionLayout.Describe(position)));
        }

        return snapshot;
    }
}
