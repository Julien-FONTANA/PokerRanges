namespace PokerRanges.Core.Table;

/// <summary>
/// An action during the hand. For a bet or a raise, <paramref name="AmountTo"/> is the total the
/// player has committed on this street once the action is done — "raise to" semantics, the ones
/// card rooms display, and the only ones that are never ambiguous.
/// </summary>
public sealed record PlayerAction(Street Street, Position Position, PlayerActionKind Kind, double AmountTo)
{
    public static PlayerAction Fold(Street street, Position position)
    {
        return new PlayerAction(street, position, PlayerActionKind.Fold, 0);
    }

    public static PlayerAction Check(Street street, Position position)
    {
        return new PlayerAction(street, position, PlayerActionKind.Check, 0);
    }

    public static PlayerAction Call(Street street, Position position)
    {
        return new PlayerAction(street, position, PlayerActionKind.Call, 0);
    }

    public static PlayerAction BetTo(Street street, Position position, double amountTo)
    {
        return new PlayerAction(street, position, PlayerActionKind.Bet, amountTo);
    }

    public static PlayerAction RaiseTo(Street street, Position position, double amountTo)
    {
        return new PlayerAction(street, position, PlayerActionKind.Raise, amountTo);
    }
}
