namespace PokerRanges.Core.Equity;

public sealed record EquityResult(
    IReadOnlyList<PlayerEquity> Players,
    long SamplesEvaluated,
    bool WasExhaustive,
    double HeroStandardError,
    TimeSpan Duration)
{
    public PlayerEquity Hero => Players[0];

    /// <summary>Demi-largeur de l'intervalle de confiance à 95 % sur l'équité du héros.</summary>
    public double HeroConfidenceMargin => 1.96 * HeroStandardError;
}
