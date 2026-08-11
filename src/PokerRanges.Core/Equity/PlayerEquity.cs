namespace PokerRanges.Core.Equity;

/// <summary>
/// Résultat d'un joueur. <see cref="Equity"/> est la part de pot espérée : elle compte les
/// partages au prorata du nombre de gagnants, contrairement à <see cref="WinRate"/>.
/// </summary>
public sealed record PlayerEquity(double Equity, double WinRate, double TieRate)
{
    public double LoseRate => Math.Max(0, 1 - WinRate - TieRate);
}
