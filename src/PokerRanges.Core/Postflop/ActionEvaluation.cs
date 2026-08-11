namespace PokerRanges.Core.Postflop;

/// <summary>
/// A candidate action and its expected value, in chips, counted from now on: folding is worth
/// zero, and whatever is already in the pot is gone either way.
/// </summary>
public sealed record ActionEvaluation
{
    public required PostflopActionKind Kind { get; init; }

    /// <summary>Extra chips committed by this action.</summary>
    public required double Amount { get; init; }

    public required double ExpectedValue { get; init; }

    /// <summary>Hero's equity against the range that continues against this action.</summary>
    public required double Equity { get; init; }

    /// <summary>
    /// Standard error on <see cref="Equity"/>. Zero when no sampling was needed — folding is worth
    /// zero without any calculation at all.
    /// </summary>
    public double EquityStandardError { get; init; }

    public required double FoldProbability { get; init; }

    public required string Label { get; init; }

    public required string Explanation { get; init; }

    public double ExpectedValueInBigBlinds(double bigBlind)
    {
        return bigBlind <= 0 ? 0 : ExpectedValue / bigBlind;
    }
}
