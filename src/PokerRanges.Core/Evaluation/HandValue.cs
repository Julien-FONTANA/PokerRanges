using PokerRanges.Core.Cards;
using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Evaluation;

/// <summary>
/// Force absolue d'une main abattue. <see cref="Strength"/> encode la catégorie puis jusqu'à cinq
/// rangs départageurs en base 15, ce qui rend deux mains comparables par un simple entier.
/// </summary>
public readonly record struct HandValue : IComparable<HandValue>
{
    private const int Radix = 15;

    private HandValue(HandCategory category, int strength)
    {
        Category = category;
        Strength = strength;
    }

    public HandCategory Category { get; }

    public int Strength { get; }

    public static HandValue Create(
        HandCategory category,
        int firstTiebreak,
        int secondTiebreak,
        int thirdTiebreak,
        int fourthTiebreak,
        int fifthTiebreak)
    {
        int strength = (int)category;
        strength = (strength * Radix) + firstTiebreak;
        strength = (strength * Radix) + secondTiebreak;
        strength = (strength * Radix) + thirdTiebreak;
        strength = (strength * Radix) + fourthTiebreak;
        strength = (strength * Radix) + fifthTiebreak;

        return new HandValue(category, strength);
    }

    public static bool operator <(HandValue left, HandValue right) => left.Strength < right.Strength;

    public static bool operator >(HandValue left, HandValue right) => left.Strength > right.Strength;

    public static bool operator <=(HandValue left, HandValue right) => left.Strength <= right.Strength;

    public static bool operator >=(HandValue left, HandValue right) => left.Strength >= right.Strength;

    public int CompareTo(HandValue other)
    {
        return Strength.CompareTo(other.Strength);
    }

    public override string ToString()
    {
        return $"{Category} ({Strength})";
    }

    internal static int TiebreakAt(int strength, int position)
    {
        int divisor = 1;
        for (int step = position; step < 5; step++)
        {
            divisor *= Radix;
        }

        return strength / divisor % Radix;
    }

    public string Describe()
    {
        int first = TiebreakAt(Strength, 1);
        int second = TiebreakAt(Strength, 2);

        return Category switch
        {
            HandCategory.StraightFlush when first == (int)Rank.Ace => HandText.RoyalFlush,
            HandCategory.StraightFlush => HandText.StraightFlushTo((Rank)first),
            HandCategory.FourOfAKind => HandText.FourOfAKind((Rank)first),
            HandCategory.FullHouse => HandText.FullHouse((Rank)first, (Rank)second),
            HandCategory.Flush => HandText.FlushTo((Rank)first),
            HandCategory.Straight => HandText.StraightTo((Rank)first),
            HandCategory.ThreeOfAKind => HandText.ThreeOfAKind((Rank)first),
            HandCategory.TwoPair => HandText.TwoPair((Rank)first, (Rank)second),
            HandCategory.OnePair => HandText.OnePair((Rank)first),
            _ => HandText.HighCard((Rank)first),
        };
    }
}
