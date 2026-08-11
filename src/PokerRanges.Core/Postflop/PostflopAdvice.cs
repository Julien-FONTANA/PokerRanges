using PokerRanges.Core.Evaluation;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Table;

namespace PokerRanges.Core.Postflop;

public sealed record PostflopAdvice
{
    public required ActionEvaluation Best { get; init; }

    public required IReadOnlyList<ActionEvaluation> Candidates { get; init; }

    public required HandFeatures HeroHand { get; init; }

    public required BoardTexture Board { get; init; }

    public required IReadOnlyList<OpponentRange> Opponents { get; init; }

    public required PotSnapshot Pot { get; init; }

    public required IReadOnlyList<string> Rationale { get; init; }

    /// <summary>
    /// True when the runner-up is within model noise: better to say so than to let it look like a
    /// clear-cut decision.
    /// </summary>
    public required bool IsClose { get; init; }

    public required bool IsHeadsUp { get; init; }

    public required PostflopBudget Budget { get; init; }

    /// <summary>
    /// The worst standard error among the equities measured for this advice. A faster answer is a
    /// less precise one: better to show the price paid than to keep quiet about it.
    /// </summary>
    public required double EquityStandardError { get; init; }

    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Half-width of the 95% interval on the equity, converted into expected chips over the pot at
    /// stake. Counts only the equity sampling, not the range ranking's.
    /// </summary>
    public double ExpectedValueMargin => 1.96 * EquityStandardError * Pot.Pot;

    public string DescribePrecision()
    {
        return PostflopText.Precision(Budget.Name, 1.96 * EquityStandardError, Duration.TotalMilliseconds);
    }
}
