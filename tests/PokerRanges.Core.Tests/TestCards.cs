using PokerRanges.Core.Cards;

namespace PokerRanges.Core.Tests;

internal static class TestCards
{
    public static Card[] Parse(string cards)
    {
        if (cards.Length % 2 != 0)
        {
            throw new ArgumentException($"Attendu un nombre pair de caractères, reçu « {cards} ».", nameof(cards));
        }

        Card[] parsed = new Card[cards.Length / 2];
        for (int index = 0; index < parsed.Length; index++)
        {
            parsed[index] = Card.Parse(cards.AsSpan(index * 2, 2));
        }

        return parsed;
    }
}
