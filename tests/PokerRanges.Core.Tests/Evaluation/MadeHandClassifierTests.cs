using PokerRanges.Core.Cards;
using PokerRanges.Core.Evaluation;
using Shouldly;

namespace PokerRanges.Core.Tests.Evaluation;

public sealed class MadeHandClassifierTests
{
    private readonly MadeHandClassifier _classifier = new(new RankCountHandEvaluator());

    [Theory]
    [InlineData("AhKd", "Kc7d2s", MadeHandTier.TopPair)]
    [InlineData("Ah7d", "Kc7d2s", MadeHandTier.MiddlePair)]
    [InlineData("Ah2d", "Kc7d2s", MadeHandTier.BottomPair)]
    [InlineData("AhAd", "Kc7d2s", MadeHandTier.Overpair)]
    [InlineData("9h9d", "Kc7d2s", MadeHandTier.UnderPair)]
    [InlineData("Ah3d", "Kc7d2s", MadeHandTier.HighCard)]
    [InlineData("Kh7s", "Kc7d2s", MadeHandTier.TwoPair)]
    [InlineData("7h7s", "Kc7d2s", MadeHandTier.Set)]
    [InlineData("Ah7s", "7c7d2s", MadeHandTier.Trips)]
    [InlineData("6h5s", "7c8d9s", MadeHandTier.Straight)]
    [InlineData("AhKh", "7h8h9h", MadeHandTier.Flush)]
    [InlineData("7h7s", "7c2d2s", MadeHandTier.FullHouse)]
    public void TheHandIsRankedRelativeToTheBoard(string hole, string board, MadeHandTier expected)
    {
        _classifier.Classify(HoleCards.Parse(hole), TestCards.Parse(board)).Tier.ShouldBe(expected);
    }

    [Fact]
    public void AFlushDrawIsWorthNineOuts()
    {
        HandFeatures features = _classifier.Classify(HoleCards.Parse("AsKs"), TestCards.Parse("Qs7s2h"));

        features.HasFlushDraw.ShouldBeTrue();
        features.Outs.ShouldBe(9);
        features.HasOpenEndedStraightDraw.ShouldBeFalse();
    }

    [Fact]
    public void AnOpenEndedStraightDrawIsWorthEightOuts()
    {
        HandFeatures features = _classifier.Classify(HoleCards.Parse("9h8h"), TestCards.Parse("7c6s2d"));

        features.HasOpenEndedStraightDraw.ShouldBeTrue();
        features.StraightOuts.ShouldBe(8);
        features.Outs.ShouldBe(8);
    }

    [Fact]
    public void AGutshotIsWorthFourOuts()
    {
        HandFeatures features = _classifier.Classify(HoleCards.Parse("AhKh"), TestCards.Parse("QsJd2c"));

        features.HasGutshot.ShouldBeTrue();
        features.HasOpenEndedStraightDraw.ShouldBeFalse();
        features.StraightOuts.ShouldBe(4);
    }

    [Fact]
    public void ACombinedDrawCountsEachCardOnlyOnce()
    {
        HandFeatures features = _classifier.Classify(HoleCards.Parse("9s8s"), TestCards.Parse("7s6s2d"));

        features.IsComboDraw.ShouldBeTrue();
        features.HasFlushDraw.ShouldBeTrue();
        features.HasOpenEndedStraightDraw.ShouldBeTrue();
        features.Outs.ShouldBe(15);
    }

    [Fact]
    public void TheRiverLeavesNoOutsAtAll()
    {
        HandFeatures features = _classifier.Classify(HoleCards.Parse("AsKs"), TestCards.Parse("Qs7s2h3d4c"));

        features.Outs.ShouldBe(0);
        features.HasFlushDraw.ShouldBeFalse();
        features.ImprovementChance(5).ShouldBe(0);
    }

    [Fact]
    public void AFlushDrawHitsRoughlyThirtyFivePercentOfTheTimeFromTheFlop()
    {
        HandFeatures features = _classifier.Classify(HoleCards.Parse("AsKs"), TestCards.Parse("Qs7s2h"));

        features.ImprovementChance(3).ShouldBe(0.35, 0.02);
    }

    [Fact]
    public void TheBestPossibleHandIsRecognised()
    {
        _classifier.Classify(HoleCards.Parse("AsKs"), TestCards.Parse("QsJsTs")).IsNuts.ShouldBeTrue();
        _classifier.Classify(HoleCards.Parse("9s9d"), TestCards.Parse("QsJsTs")).IsNuts.ShouldBeFalse();
    }

    [Fact]
    public void HoldingTheBlockerIsEnoughToBeUnbeatable()
    {
        HandFeatures features = _classifier.Classify(HoleCards.Parse("KhKs"), TestCards.Parse("KdKc2h"));

        features.Tier.ShouldBe(MadeHandTier.Quads);
        features.IsNuts.ShouldBeTrue();
    }

    [Fact]
    public void TheDescriptionNamesBothTheMadeHandAndTheDraw()
    {
        string description = _classifier.Classify(HoleCards.Parse("9s8s"), TestCards.Parse("7s6s2d")).Describe();

        description.ShouldContain("flush draw");
        description.ShouldContain("open-ended");
    }
}
