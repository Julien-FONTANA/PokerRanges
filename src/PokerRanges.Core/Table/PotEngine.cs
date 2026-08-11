using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace PokerRanges.Core.Table;

public sealed class PotEngine : IPotEngine
{
    private readonly ILogger<PotEngine> _logger;

    public PotEngine(ILogger<PotEngine> logger)
    {
        _logger = logger;
    }

    public HandAnalysis Analyse(HandState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        HandReplay replay = HandReplay.Run(state);
        ImmutableArray<Position> live = replay.LivePositions();

        Dictionary<Position, PotSnapshot> snapshots = [];
        foreach (Position seat in PositionLayout.Seats(state.Table.PlayerCount))
        {
            snapshots[seat] = BuildSnapshot(replay, seat, live);
        }

        _logger.LogDebug(
            "Hand analysed on the {Street}: pot {Pot}, current bet {CurrentBet}, {LiveCount} players still in, {NextToAct} to act.",
            replay.Street,
            replay.Pot,
            replay.CurrentBet,
            live.Length,
            replay.NextToAct());

        return new HandAnalysis
        {
            Street = replay.Street,
            Pot = replay.Pot,
            CurrentBet = replay.CurrentBet,
            HeroPosition = state.Table.HeroPosition,
            NextToAct = replay.NextToAct(),
            LivePositions = live,
            Snapshots = snapshots,
        };
    }

    private static PotSnapshot BuildSnapshot(HandReplay replay, Position position, ImmutableArray<Position> live)
    {
        double remaining = replay.RemainingOf(position);
        double committed = replay.CommittedBy(position);
        double deepestOpponent = 0;
        double deepestOpponentStart = 0;

        foreach (Position opponent in live)
        {
            if (opponent == position)
            {
                continue;
            }

            deepestOpponent = Math.Max(deepestOpponent, replay.RemainingOf(opponent));
            deepestOpponentStart = Math.Max(
                deepestOpponentStart,
                replay.RemainingOf(opponent) + replay.CommittedBy(opponent));
        }

        bool isLive = live.Contains(position);

        return new PotSnapshot
        {
            Position = position,
            Pot = replay.Pot,
            AmountToCall = replay.AmountToCallFor(position),
            Committed = committed,
            StreetCommitted = replay.StreetCommittedBy(position),
            RemainingStack = remaining,
            EffectiveStack = isLive ? Math.Min(remaining, deepestOpponent) : 0,
            EffectiveStartingStack = isLive ? Math.Min(remaining + committed, deepestOpponentStart) : 0,
            BigBlind = replay.Table.BigBlind,
        };
    }
}
