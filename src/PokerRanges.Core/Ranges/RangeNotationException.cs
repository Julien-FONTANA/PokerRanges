namespace PokerRanges.Core.Ranges;

public sealed class RangeNotationException : PokerRangesException
{
    public RangeNotationException(string token, string reason)
        : base($"Invalid range notation at \"{token}\": {reason}")
    {
        Token = token;
        Reason = reason;
    }

    public string Token { get; }

    public string Reason { get; }
}
