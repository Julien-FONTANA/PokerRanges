namespace PokerRanges.Core.Cards;

public sealed class CardFormatException : PokerRangesException
{
    public CardFormatException(string message)
        : base(message)
    {
    }
}
