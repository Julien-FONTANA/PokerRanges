namespace PokerRanges.Core.Equity;

public enum EquityMethod
{
    /// <summary>Exhaustive enumeration when the volume allows it, Monte-Carlo otherwise.</summary>
    Automatic,
    Exhaustive,
    MonteCarlo,
}
