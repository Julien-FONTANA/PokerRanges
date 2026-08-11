using PokerRanges.Core.Table;

namespace PokerRanges.Data.Storage;

/// <summary>
/// The on-disk form of a hand. Deliberately distinct from <see cref="HandState"/>: the engine must
/// be free to change its internal representation without making already-saved hands unreadable.
/// Cards are text here — "Ks" — so a resume file stays readable by eye.
/// </summary>
public sealed class StoredHand
{
    public int PlayerCount { get; set; } = 8;

    public double BigBlind { get; set; } = 8;

    public double? SmallBlind { get; set; }

    public AnteStyle AnteStyle { get; set; } = AnteStyle.None;

    public double AnteAmount { get; set; }

    public Position HeroPosition { get; set; } = Position.Button;

    public Dictionary<Position, double> StartingStacks { get; set; } = [];

    public string? HeroCards { get; set; }

    public string Board { get; set; } = string.Empty;

    public List<StoredAction> Actions { get; set; } = [];
}
