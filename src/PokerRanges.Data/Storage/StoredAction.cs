using PokerRanges.Core.Table;

namespace PokerRanges.Data.Storage;

public sealed class StoredAction
{
    public Street Street { get; set; }

    public Position Position { get; set; }

    public PlayerActionKind Kind { get; set; }

    /// <summary>Total engagé sur la street après l'action, comme dans <see cref="PlayerAction"/>.</summary>
    public double AmountTo { get; set; }
}
