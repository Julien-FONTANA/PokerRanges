namespace PokerRanges.Core.Cards;

/// <summary>
/// A player's two hole cards, one of the 1326 combos. The order is normalised
/// (<see cref="First"/> always holds the higher index) so that two instances describing the same
/// hand compare equal however they were built.
/// </summary>
public readonly record struct HoleCards
{
    public const int Count = 1326;

    public HoleCards(Card first, Card second)
    {
        if (first == second)
        {
            throw new ArgumentException($"A combo requires two distinct cards, got {first} twice.", nameof(second));
        }

        bool firstIsHigher = first.Index > second.Index;
        First = firstIsHigher ? first : second;
        Second = firstIsHigher ? second : first;
    }

    public Card First { get; }

    public Card Second { get; }

    public int Index => (First.Index * (First.Index - 1) / 2) + Second.Index;

    public static HoleCards FromIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

        int high = (int)((1 + Math.Sqrt(1 + (8.0 * index))) / 2);
        while (high * (high - 1) / 2 > index)
        {
            high--;
        }

        while ((high + 1) * high / 2 <= index)
        {
            high++;
        }

        return new HoleCards(Card.FromIndex(high), Card.FromIndex(index - (high * (high - 1) / 2)));
    }

    public static IEnumerable<HoleCards> All()
    {
        for (int index = 0; index < Count; index++)
        {
            yield return FromIndex(index);
        }
    }

    public static HoleCards Parse(ReadOnlySpan<char> text)
    {
        if (!TryParse(text, out HoleCards combo))
        {
            throw new CardFormatException($"Invalid combo: \"{text}\". Expected format: two distinct cards, for example \"AsKh\".");
        }

        return combo;
    }

    public static bool TryParse(ReadOnlySpan<char> text, out HoleCards combo)
    {
        combo = default;
        ReadOnlySpan<char> trimmed = text.Trim();

        if (trimmed.Length != 4
            || !Card.TryParse(trimmed[..2], out Card first)
            || !Card.TryParse(trimmed[2..], out Card second)
            || first == second)
        {
            return false;
        }

        combo = new HoleCards(first, second);
        return true;
    }

    public bool Contains(Card card)
    {
        return First == card || Second == card;
    }

    public bool Intersects(ReadOnlySpan<Card> cards)
    {
        foreach (Card card in cards)
        {
            if (Contains(card))
            {
                return true;
            }
        }

        return false;
    }

    public HandClass ToHandClass()
    {
        if (First.Rank == Second.Rank)
        {
            return HandClass.Pair(First.Rank);
        }

        HandShape shape = First.Suit == Second.Suit ? HandShape.Suited : HandShape.Offsuit;
        return new HandClass(First.Rank, Second.Rank, shape);
    }

    public override string ToString()
    {
        return First.ToString() + Second.ToString();
    }
}
