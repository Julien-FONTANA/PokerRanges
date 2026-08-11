using System.Collections.Immutable;
using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Table;

/// <summary>
/// Who sits where, and in what order they act, for 2 to 8 players. The tables are written out
/// rather than derived from a rule: real seat naming is not regular (six-handed starts at UTG,
/// five-handed at HJ), and explicit data reads and tests better.
/// </summary>
public static class PositionLayout
{
    public const int MinimumPlayers = 2;
    public const int MaximumPlayers = 8;

    private static readonly ImmutableArray<ImmutableArray<Position>> SeatsByPlayerCount =
    [
        [],
        [],
        [Position.SmallBlind, Position.BigBlind],
        [Position.SmallBlind, Position.BigBlind, Position.Button],
        [Position.SmallBlind, Position.BigBlind, Position.CutOff, Position.Button],
        [Position.SmallBlind, Position.BigBlind, Position.HiJack, Position.CutOff, Position.Button],
        [Position.SmallBlind, Position.BigBlind, Position.UnderTheGun, Position.HiJack, Position.CutOff, Position.Button],
        [Position.SmallBlind, Position.BigBlind, Position.UnderTheGun, Position.LoJack, Position.HiJack, Position.CutOff, Position.Button],
        [Position.SmallBlind, Position.BigBlind, Position.UnderTheGun, Position.UnderTheGunPlusOne, Position.LoJack, Position.HiJack, Position.CutOff, Position.Button],
    ];

    /// <summary>The seats in dealing order, starting from the small blind.</summary>
    public static ImmutableArray<Position> Seats(int playerCount)
    {
        EnsureSupported(playerCount);
        return SeatsByPlayerCount[playerCount];
    }

    /// <summary>
    /// Preflop action order: it starts to the left of the big blind, who acts last. Heads-up, the
    /// small blind is also the button and opens the action.
    /// </summary>
    public static ImmutableArray<Position> PreflopOrder(int playerCount)
    {
        ImmutableArray<Position> seats = Seats(playerCount);
        return [.. seats.Skip(2), .. seats.Take(2)];
    }

    /// <summary>
    /// Postflop action order: the small blind opens. Heads-up it is also the button, so the big
    /// blind acts first.
    /// </summary>
    public static ImmutableArray<Position> PostflopOrder(int playerCount)
    {
        ImmutableArray<Position> seats = Seats(playerCount);
        return playerCount == 2 ? [Position.BigBlind, Position.SmallBlind] : seats;
    }

    public static ImmutableArray<Position> ActionOrder(int playerCount, Street street)
    {
        return street == Street.Preflop ? PreflopOrder(playerCount) : PostflopOrder(playerCount);
    }

    public static bool IsSeated(int playerCount, Position position)
    {
        return playerCount is >= MinimumPlayers and <= MaximumPlayers
               && SeatsByPlayerCount[playerCount].Contains(position);
    }

    /// <summary>
    /// How many players act after this one on the first betting round. This is the dimension that
    /// really indexes opening charts: opening with three players behind poses the same problem at
    /// a five-handed table and at an eight-handed one.
    /// </summary>
    public static int PlayersLeftToActPreflop(int playerCount, Position position)
    {
        ImmutableArray<Position> order = PreflopOrder(playerCount);
        int index = order.IndexOf(position);

        if (index < 0)
        {
            throw new TableException(TableText.SeatNotAtTable(position, playerCount));
        }

        return order.Length - index - 1;
    }

    /// <summary>True if <paramref name="position"/> acts after <paramref name="other"/> once the flop is out.</summary>
    public static bool ActsAfterPostflop(int playerCount, Position position, Position other)
    {
        ImmutableArray<Position> order = PostflopOrder(playerCount);
        return order.IndexOf(position) > order.IndexOf(other);
    }

    public static string Describe(Position position)
    {
        return position switch
        {
            Position.UnderTheGun => "UTG",
            Position.UnderTheGunPlusOne => "UTG+1",
            Position.LoJack => "LJ",
            Position.HiJack => "HJ",
            Position.CutOff => "CO",
            Position.Button => "BTN",
            Position.SmallBlind => "SB",
            _ => "BB",
        };
    }

    private static void EnsureSupported(int playerCount)
    {
        if (playerCount is < MinimumPlayers or > MaximumPlayers)
        {
            throw new TableException(TableText.PlayerCountOutOfRange(MinimumPlayers, MaximumPlayers, playerCount));
        }
    }
}
