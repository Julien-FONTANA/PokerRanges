namespace PokerRanges.Core.Preflop;

public sealed class PreflopChartException : PokerRangesException
{
    public PreflopChartException(string message)
        : base(message)
    {
    }

    public PreflopChartException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
