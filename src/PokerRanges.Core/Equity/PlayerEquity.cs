namespace PokerRanges.Core.Equity;

/// <summary>
/// One player's result. <see cref="Equity"/> is the expected share of the pot: it counts split
/// pots pro rata to the number of winners, unlike <see cref="WinRate"/>.
/// </summary>
public sealed record PlayerEquity(double Equity, double WinRate, double TieRate)
{
    public double LoseRate => Math.Max(0, 1 - WinRate - TieRate);
}
