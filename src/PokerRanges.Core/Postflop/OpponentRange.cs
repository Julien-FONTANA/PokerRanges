using PokerRanges.Core.Ranges;
using PokerRanges.Core.Table;

namespace PokerRanges.Core.Postflop;

/// <summary>
/// The range assigned to an opponent, with the story that produced it: which preflop chart it
/// starts from and how each of their actions narrowed it. Without that story, an assigned range
/// is just an assertion.
/// </summary>
public sealed record OpponentRange(Position Position, HandRange Range, IReadOnlyList<string> Story)
{
    public double Combos => Range.TotalCombos;
}
