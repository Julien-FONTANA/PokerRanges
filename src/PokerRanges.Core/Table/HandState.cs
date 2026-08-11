using PokerRanges.Core.Cards;
using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Table;

public sealed record HandState
{
    public required TableConfiguration Table { get; init; }

    public HoleCards? HeroCards { get; init; }

    public IReadOnlyList<Card> Board { get; init; } = [];

    public IReadOnlyList<PlayerAction> Actions { get; init; } = [];

    public Street CurrentStreet => Board.Count switch
    {
        0 => Street.Preflop,
        3 => Street.Flop,
        4 => Street.Turn,
        5 => Street.River,
        _ => throw new TableException(TableText.BoardCardCount(Board.Count)),
    };

    public HandState With(PlayerAction action)
    {
        return this with { Actions = [.. Actions, action] };
    }

    public HandState WithBoard(params Card[] board)
    {
        return this with { Board = board };
    }
}
