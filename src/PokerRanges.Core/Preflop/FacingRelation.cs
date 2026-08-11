namespace PokerRanges.Core.Preflop;

/// <summary>
/// Where the hero sits relative to the aggressor. The blinds are kept distinct from plain
/// position because the money they have already posted changes the defence odds entirely.
/// </summary>
public enum FacingRelation
{
    InPosition,
    OutOfPosition,
    SmallBlind,
    BigBlind,
}
