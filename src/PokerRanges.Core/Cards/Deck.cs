using System.Collections.Immutable;

namespace PokerRanges.Core.Cards;

public static class Deck
{
    public static ImmutableArray<Card> AllCards { get; } = BuildAllCards();

    public static ImmutableArray<Rank> RanksHighToLow { get; } =
    [
        Rank.Ace, Rank.King, Rank.Queen, Rank.Jack, Rank.Ten, Rank.Nine, Rank.Eight,
        Rank.Seven, Rank.Six, Rank.Five, Rank.Four, Rank.Three, Rank.Two,
    ];

    public static ImmutableArray<Suit> AllSuits { get; } =
        [Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades];

    private static ImmutableArray<Card> BuildAllCards()
    {
        ImmutableArray<Card>.Builder builder = ImmutableArray.CreateBuilder<Card>(Card.Count);
        for (int index = 0; index < Card.Count; index++)
        {
            builder.Add(Card.FromIndex(index));
        }

        return builder.MoveToImmutable();
    }
}
