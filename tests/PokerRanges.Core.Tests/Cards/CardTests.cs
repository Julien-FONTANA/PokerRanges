using PokerRanges.Core.Cards;
using Shouldly;

namespace PokerRanges.Core.Tests.Cards;

public sealed class CardTests
{
    [Fact]
    public void EveryIndexRoundTripsThroughTheCardItself()
    {
        for (int index = 0; index < Card.Count; index++)
        {
            Card card = Card.FromIndex(index);
            card.Index.ShouldBe(index);
        }
    }

    [Fact]
    public void EveryCardRoundTripsThroughItsTextForm()
    {
        foreach (Card card in Deck.AllCards)
        {
            Card.Parse(card.ToString()).ShouldBe(card);
        }
    }

    [Fact]
    public void TheDeckHoldsFiftyTwoDistinctCards()
    {
        Deck.AllCards.Length.ShouldBe(52);
        Deck.AllCards.Distinct().Count().ShouldBe(52);
    }

    [Theory]
    [InlineData("As", Rank.Ace, Suit.Spades)]
    [InlineData("2c", Rank.Two, Suit.Clubs)]
    [InlineData("Th", Rank.Ten, Suit.Hearts)]
    [InlineData("kd", Rank.King, Suit.Diamonds)]
    public void ParsingAcceptsTheStandardTwoCharacterForm(string text, Rank expectedRank, Suit expectedSuit)
    {
        Card card = Card.Parse(text);

        card.Rank.ShouldBe(expectedRank);
        card.Suit.ShouldBe(expectedSuit);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("Axs")]
    [InlineData("1s")]
    [InlineData("Az")]
    public void ParsingRejectsMalformedInput(string text)
    {
        Should.Throw<CardFormatException>(() => Card.Parse(text));
    }

    [Fact]
    public void IndexOrdersCardsByRankThenSuit()
    {
        new Card(Rank.Two, Suit.Clubs).Index.ShouldBe(0);
        new Card(Rank.Ace, Suit.Spades).Index.ShouldBe(51);
        new Card(Rank.Three, Suit.Clubs).Index.ShouldBeGreaterThan(new Card(Rank.Two, Suit.Spades).Index);
    }
}
