using PokerRanges.Core.Cards;
using PokerRanges.Core.Evaluation;
using Shouldly;

namespace PokerRanges.Core.Tests.Evaluation;

public sealed class HandEvaluatorTests
{
    private readonly RankCountHandEvaluator _evaluator = new();

    [Theory]
    [InlineData("AsKsQsJsTs", HandCategory.StraightFlush)]
    [InlineData("5s4s3s2sAs", HandCategory.StraightFlush)]
    [InlineData("AsAhAdAc2s", HandCategory.FourOfAKind)]
    [InlineData("AsAhAdKcKs", HandCategory.FullHouse)]
    [InlineData("As9s7s4s2s", HandCategory.Flush)]
    [InlineData("AsKhQdJcTs", HandCategory.Straight)]
    [InlineData("5s4h3d2cAs", HandCategory.Straight)]
    [InlineData("AsAhAd7c2s", HandCategory.ThreeOfAKind)]
    [InlineData("AsAhKdKc2s", HandCategory.TwoPair)]
    [InlineData("AsAh9d7c2s", HandCategory.OnePair)]
    [InlineData("AsQh9d7c2s", HandCategory.HighCard)]
    public void EachFiveCardHandIsPlacedInItsCategory(string cards, HandCategory expected)
    {
        _evaluator.Evaluate(TestCards.Parse(cards)).Category.ShouldBe(expected);
    }

    [Fact]
    public void TheWheelIsTheLowestStraightAndTheBroadwayTheHighest()
    {
        HandValue wheel = _evaluator.Evaluate(TestCards.Parse("5s4h3d2cAs"));
        HandValue sixHigh = _evaluator.Evaluate(TestCards.Parse("6s5h4d3c2s"));
        HandValue broadway = _evaluator.Evaluate(TestCards.Parse("AsKhQdJcTs"));

        wheel.ShouldBeLessThan(sixHigh);
        sixHigh.ShouldBeLessThan(broadway);
    }

    [Fact]
    public void KickersDepartTwoOtherwiseIdenticalHands()
    {
        HandValue aceKicker = _evaluator.Evaluate(TestCards.Parse("KsKh9d7cAs"));
        HandValue queenKicker = _evaluator.Evaluate(TestCards.Parse("KsKh9d7cQs"));

        aceKicker.ShouldBeGreaterThan(queenKicker);
    }

    [Fact]
    public void TheHigherTripsWinsAFullHouseComparison()
    {
        HandValue acesFullOfKings = _evaluator.Evaluate(TestCards.Parse("AsAhAdKcKs"));
        HandValue kingsFullOfAces = _evaluator.Evaluate(TestCards.Parse("KsKhKdAcAs"));

        acesFullOfKings.ShouldBeGreaterThan(kingsFullOfAces);
    }

    [Fact]
    public void TheCategoriesAreOrderedFromHighCardToStraightFlush()
    {
        string[] ascending =
        [
            "AsQh9d7c2s", "AsAh9d7c2s", "AsAhKdKc2s", "AsAhAd7c2s",
            "AsKhQdJcTs", "As9s7s4s2s", "AsAhAdKcKs", "AsAhAdAc2s", "AsKsQsJsTs",
        ];

        HandValue[] values = [.. ascending.Select(hand => _evaluator.Evaluate(TestCards.Parse(hand)))];

        for (int index = 1; index < values.Length; index++)
        {
            values[index].ShouldBeGreaterThan(values[index - 1]);
        }
    }

    [Fact]
    public void SevenCardsKeepTheBestFiveIncludingWhenTheBoardPlaysAlone()
    {
        _evaluator.Evaluate(TestCards.Parse("AsKsQsJsTs2h3h")).Category.ShouldBe(HandCategory.StraightFlush);
        _evaluator.Evaluate(TestCards.Parse("2c7d2sTsJs4s8s")).Category.ShouldBe(HandCategory.Flush);
        _evaluator.Evaluate(TestCards.Parse("3s3h3d4s4h4d2c")).Describe().ShouldBe("Full house, 4s full of 3s");
        _evaluator.Evaluate(TestCards.Parse("3s3h3d3c4s4h2c")).Describe().ShouldBe("Four of a kind, 3s");
    }

    [Fact]
    public void AFlushBeatsAStraightWhenBothAreAvailable()
    {
        HandValue value = _evaluator.Evaluate(TestCards.Parse("9s8s7s6s2s5h"));

        value.Category.ShouldBe(HandCategory.Flush);
    }

    [Fact]
    public void ARoyalFlushIsNamedAsSuch()
    {
        _evaluator.Evaluate(TestCards.Parse("AsKsQsJsTs")).Describe().ShouldBe("Royal flush");
        _evaluator.Evaluate(TestCards.Parse("5s4s3s2sAs")).Describe().ShouldBe("Straight flush to 5");
    }

    [Theory]
    [InlineData("AsKsQsJs")]
    [InlineData("AsKsQsJsTs9s8s7s")]
    public void AHandOutsideOfFiveToSevenCardsIsRejected(string cards)
    {
        Card[] parsed = TestCards.Parse(cards);

        Should.Throw<ArgumentException>(() => _evaluator.Evaluate(parsed));
    }

    [Fact]
    public void TheHoleCardsAndBoardOverloadMatchesTheDirectEvaluation()
    {
        Card[] board = TestCards.Parse("QsJsTs2h3h");
        HandValue viaExtension = _evaluator.EvaluateHand(HoleCards.Parse("AsKs"), board);
        HandValue direct = _evaluator.Evaluate(TestCards.Parse("AsKsQsJsTs2h3h"));

        viaExtension.ShouldBe(direct);
    }
}
