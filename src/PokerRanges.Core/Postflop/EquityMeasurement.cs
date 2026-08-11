namespace PokerRanges.Core.Postflop;

/// <summary>
/// A measured equity and what it is worth. The measurement and its uncertainty travel together:
/// separating them loses the second along the way and presents an estimate as a fact.
/// </summary>
public sealed record EquityMeasurement(double Equity, double StandardError)
{
    public static EquityMeasurement Certain(double equity)
    {
        return new EquityMeasurement(equity, 0);
    }
}
