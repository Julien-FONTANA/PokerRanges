using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Preflop;

/// <summary>
/// The chart actually used to answer, and the list of gaps between what was asked for and what
/// existed. No advice should leave the application without a way to trace it back to the data
/// that produced it.
/// </summary>
public sealed record ChartResolution(
    PreflopChart Chart,
    ChartKey Requested,
    RangeStrategy Strategy,
    IReadOnlyList<string> Adjustments)
{
    public bool IsExactMatch => Adjustments.Count == 0;

    public string Describe()
    {
        return IsExactMatch
            ? PreflopText.ChartPrefix(Chart.Describe())
            : PreflopText.ChartPrefixWithAdjustments(
                Chart.Describe(),
                string.Join(PreflopText.AdjustmentSeparator, Adjustments));
    }
}
