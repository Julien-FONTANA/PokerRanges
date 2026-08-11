using System.Collections.Immutable;
using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Table;

/// <summary>
/// Qui est assis où, et dans quel ordre on parle, pour 2 à 8 joueurs. Les tables sont écrites en
/// dur plutôt que dérivées d'une règle : la nomenclature réelle n'est pas régulière (un 6-max
/// commence à UTG, un 5 joueurs à HJ) et une donnée explicite se relit et se teste mieux.
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

    /// <summary>Les sièges dans le sens de la donne, en partant de la petite blinde.</summary>
    public static ImmutableArray<Position> Seats(int playerCount)
    {
        EnsureSupported(playerCount);
        return SeatsByPlayerCount[playerCount];
    }

    /// <summary>
    /// L'ordre de parole préflop : on commence à gauche de la grosse blinde, qui parle en dernier.
    /// En tête-à-tête la petite blinde est aussi le bouton et ouvre les hostilités.
    /// </summary>
    public static ImmutableArray<Position> PreflopOrder(int playerCount)
    {
        ImmutableArray<Position> seats = Seats(playerCount);
        return [.. seats.Skip(2), .. seats.Take(2)];
    }

    /// <summary>
    /// L'ordre de parole postflop : la petite blinde ouvre. En tête-à-tête elle est le bouton,
    /// donc c'est la grosse blinde qui parle la première.
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
    /// Le nombre de joueurs qui parlent après celui-ci au premier tour d'enchères. C'est la
    /// dimension qui indexe réellement les charts d'ouverture : ouvrir avec trois joueurs derrière
    /// pose le même problème à une table de cinq et à une table de huit.
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

    /// <summary>Vrai si <paramref name="position"/> parle après <paramref name="other"/> une fois le flop étalé.</summary>
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
