using PokerRanges.Core.Cards;
using Shouldly;

namespace PokerRanges.Core.Tests.Cards;

public sealed class HoleCardsTests
{
    [Fact]
    public void TheOrderOfTheTwoCardsDoesNotChangeTheCombo()
    {
        HoleCards oneWay = new(Card.Parse("As"), Card.Parse("Kh"));
        HoleCards otherWay = new(Card.Parse("Kh"), Card.Parse("As"));

        otherWay.ShouldBe(oneWay);
        otherWay.Index.ShouldBe(oneWay.Index);
        otherWay.GetHashCode().ShouldBe(oneWay.GetHashCode());
    }

    [Fact]
    public void TheIndexCoversTheOneThousandThreeHundredTwentySixCombosExactlyOnce()
    {
        HashSet<int> indexes = [];

        foreach (Card first in Deck.AllCards)
        {
            foreach (Card second in Deck.AllCards)
            {
                if (first != second)
                {
                    indexes.Add(new HoleCards(first, second).Index);
                }
            }
        }

        indexes.Count.ShouldBe(HoleCards.Count);
        indexes.Min().ShouldBe(0);
        indexes.Max().ShouldBe(HoleCards.Count - 1);
    }

    [Fact]
    public void EveryIndexRoundTripsThroughTheComboItself()
    {
        for (int index = 0; index < HoleCards.Count; index++)
        {
            HoleCards.FromIndex(index).Index.ShouldBe(index);
        }
    }

    [Fact]
    public void EveryComboRoundTripsThroughItsTextForm()
    {
        foreach (HoleCards combo in HoleCards.All())
        {
            HoleCards.Parse(combo.ToString()).ShouldBe(combo);
        }
    }

    [Fact]
    public void TwoIdenticalCardsAreRejected()
    {
        Should.Throw<ArgumentException>(() => new HoleCards(Card.Parse("As"), Card.Parse("As")));
    }

    [Fact]
    public void ADeadCardIsDetectedInsideTheCombo()
    {
        HoleCards combo = HoleCards.Parse("AsKh");

        combo.Contains(Card.Parse("As")).ShouldBeTrue();
        combo.Contains(Card.Parse("Ah")).ShouldBeFalse();
        combo.Intersects(TestCards.Parse("2c3cKh")).ShouldBeTrue();
        combo.Intersects(TestCards.Parse("2c3c4c")).ShouldBeFalse();
    }
}
