using PokerRanges.Core.Cards;
using Shouldly;

namespace PokerRanges.Core.Tests.Cards;

public sealed class HandClassTests
{
    [Fact]
    public void TheGridHoldsTheOneHundredSixtyNineDistinctHandClasses()
    {
        HandClass.All.Length.ShouldBe(169);
        HandClass.All.Distinct().Count().ShouldBe(169);
    }

    [Fact]
    public void TheCombinationCountsAddUpToTheOneThousandThreeHundredTwentySixCombos()
    {
        HandClass.All.Sum(handClass => handClass.CombinationCount).ShouldBe(1326);
    }

    [Theory]
    [InlineData("AKs", 4)]
    [InlineData("AKo", 12)]
    [InlineData("QQ", 6)]
    [InlineData("72o", 12)]
    public void EachShapeProducesItsExpectedNumberOfCombos(string notation, int expectedCount)
    {
        HandClass handClass = HandClass.Parse(notation);

        handClass.CombinationCount.ShouldBe(expectedCount);
        handClass.Combos().Count().ShouldBe(expectedCount);
        handClass.Combos().Distinct().Count().ShouldBe(expectedCount);
    }

    [Fact]
    public void EveryComboOfAClassMapsBackToThatClass()
    {
        foreach (HandClass handClass in HandClass.All)
        {
            foreach (HoleCards combo in handClass.Combos())
            {
                combo.ToHandClass().ShouldBe(handClass);
            }
        }
    }

    [Fact]
    public void EveryHandClassRoundTripsThroughItsTextForm()
    {
        foreach (HandClass handClass in HandClass.All)
        {
            HandClass.Parse(handClass.ToString()).ShouldBe(handClass);
        }
    }

    [Fact]
    public void TheGridPlacesPairsOnTheDiagonalSuitedAboveAndOffsuitBelow()
    {
        HandClass.FromGrid(0, 0).ShouldBe(HandClass.Pair(Rank.Ace));
        HandClass.FromGrid(12, 12).ShouldBe(HandClass.Pair(Rank.Two));
        HandClass.FromGrid(0, 1).ShouldBe(HandClass.Suited(Rank.Ace, Rank.King));
        HandClass.FromGrid(1, 0).ShouldBe(HandClass.Offsuit(Rank.Ace, Rank.King));
    }

    [Fact]
    public void EveryHandClassRoundTripsThroughItsGridCoordinates()
    {
        foreach (HandClass handClass in HandClass.All)
        {
            HandClass.FromGrid(handClass.GridRow, handClass.GridColumn).ShouldBe(handClass);
        }
    }

    [Theory]
    [InlineData("AAs")]
    [InlineData("AK")]
    [InlineData("AKx")]
    [InlineData("A")]
    public void ParsingRejectsMalformedNotation(string notation)
    {
        Should.Throw<CardFormatException>(() => HandClass.Parse(notation));
    }
}
