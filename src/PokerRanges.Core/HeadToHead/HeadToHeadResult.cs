using PokerRanges.Core.Equity;
using PokerRanges.Core.Localization;

namespace PokerRanges.Core.HeadToHead;

public sealed record HeadToHeadResult
{
    public required HeadToHeadSpot Spot { get; init; }

    public required PlayerEquity Hero { get; init; }

    public required PlayerEquity Villain { get; init; }

    /// <summary>Zero when every runout was enumerated rather than sampled.</summary>
    public required double StandardError { get; init; }

    public required bool WasExhaustive { get; init; }

    public required long SamplesEvaluated { get; init; }

    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// The share of all the hands the villain could hold that he continues with. One when he cannot
    /// fold, is pinned to a single hand, or has already jammed.
    /// </summary>
    public required double VillainContinueFrequency { get; init; }

    /// <summary>
    /// The fold frequency at which a jam is worth exactly zero. Null when the question does not
    /// arise: the hero is calling, the villain cannot fold, or the jam already shows a profit
    /// against a villain who never folds.
    /// </summary>
    public required double? BreakEvenFoldFrequency { get; init; }

    /// <summary>Best first.</summary>
    public required IReadOnlyList<HeadToHeadActionEvaluation> Actions { get; init; }

    public required IReadOnlyList<string> Rationale { get; init; }

    public HeadToHeadActionEvaluation Best => Actions[0];

    /// <summary>Half-width of the 95% interval on the equity.</summary>
    public double EquityMargin => 1.96 * StandardError;

    public string DescribePrecision()
    {
        return WasExhaustive
            ? HeadToHeadText.PrecisionExhaustive(Duration.TotalMilliseconds)
            : HeadToHeadText.Precision(EquityMargin, Duration.TotalMilliseconds);
    }
}
