namespace PokerRanges.Core.Table;

/// <summary>
/// Une action au cours de la main. Pour une mise ou une relance, <paramref name="AmountTo"/> est le
/// total engagé par le joueur sur cette street une fois l'action faite — la sémantique « relance à »,
/// celle qu'affichent les salles, et la seule qui ne soit jamais ambiguë.
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
