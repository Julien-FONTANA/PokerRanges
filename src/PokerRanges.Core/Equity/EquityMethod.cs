namespace PokerRanges.Core.Equity;

public enum EquityMethod
{
    /// <summary>Énumération exhaustive si le volume le permet, Monte-Carlo sinon.</summary>
    Automatic,
    Exhaustive,
    MonteCarlo,
}
