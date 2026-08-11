using PokerRanges.Core.Table;

namespace PokerRanges.Data.Storage;

public sealed class StoredAction
{
    public Street Street { get; set; }

    public Position Position { get; set; }

    public PlayerActionKind Kind { get; set; }

    /// <summary>Total committed on the street after the action, as in <see cref="PlayerAction"/>.</summary>
    public double AmountTo { get; set; }
}
