using PokerRanges.Core.Ranges;

namespace PokerRanges.Core.Postflop;

/// <summary>
/// La range adverse découpée en trois face à une mise donnée. <see cref="Continuing"/> est la
/// réunion des mains qui suivent et de celles qui relancent : c'est contre elle, et non contre la
/// range de départ, qu'il faut mesurer son équité une fois la mise payée.
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
