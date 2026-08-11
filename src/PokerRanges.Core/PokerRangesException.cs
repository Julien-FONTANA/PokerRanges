namespace PokerRanges.Core;

public abstract class PokerRangesException : Exception
{
    protected PokerRangesException(string message)
        : base(message)
    {
    }

    protected PokerRangesException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
