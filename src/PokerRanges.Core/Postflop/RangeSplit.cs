using PokerRanges.Core.Ranges;

namespace PokerRanges.Core.Postflop;

/// <summary>
/// The opponent's range cut in three against a given bet. <see cref="Continuing"/> is the union of
/// the hands that call and those that raise: it is against that, and not against the starting
/// range, that equity must be measured once the bet is called.
/// </summary>
public sealed record RangeSplit(
    HandRange Folding,
    HandRange Calling,
    HandRange Raising,
    HandRange Continuing,
    double FoldProbability,
    double CallProbability,
    double RaiseProbability)
{
    public static RangeSplit Empty { get; } = new(
        HandRange.Empty,
        HandRange.Empty,
        HandRange.Empty,
        HandRange.Empty,
        1,
        0,
        0);
}
