namespace PokerRanges.Core.Equity;

public sealed record EquityResult(
    IReadOnlyList<PlayerEquity> Players,
    long SamplesEvaluated,
    bool WasExhaustive,
    double HeroStandardError,
    TimeSpan Duration)
{
    public PlayerEquity Hero => Players[0];

    /// <summary>Half-width of the 95% confidence interval on the hero's equity.</summary>
    public double HeroConfidenceMargin => 1.96 * HeroStandardError;
}
