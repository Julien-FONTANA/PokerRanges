using PokerRanges.Core.Cards;
using PokerRanges.Core.Postflop;
using Shouldly;

namespace PokerRanges.Core.Tests.Postflop;

public sealed class OpponentResponseModelTests
{
    private static readonly IReadOnlyList<RankedCombo> Ranked = BuildRanked(100);

    [Fact]
    public void TheThreeBucketsAddUpToTheWholeRange()
    {
        RangeSplit split = OpponentResponseModel.SplitFacingBet(Ranked, 100, 50, OpponentProfile.Balanced);

        double total = split.Folding.TotalCombos + split.Calling.TotalCombos + split.Raising.TotalCombos;

        total.ShouldBe(Ranked.Count, 1e-6);
        (split.FoldProbability + split.CallProbability + split.RaiseProbability).ShouldBe(1, 1e-9);
    }

    [Fact]
    public void ABalancedOpponentDefendsExactlyTheMinimumDefenceFrequency()
    {
        RangeSplit split = OpponentResponseModel.SplitFacingBet(Ranked, 100, 50, OpponentProfile.Balanced);

        (split.CallProbability + split.RaiseProbability).ShouldBe(100.0 / 150.0, 0.01);
    }

    [Fact]
    public void TheBiggerTheBetTheMoreHeFolds()
    {
        double smallBet = OpponentResponseModel
            .SplitFacingBet(Ranked, 100, 25, OpponentProfile.Balanced).FoldProbability;
        double bigBet = OpponentResponseModel
            .SplitFacingBet(Ranked, 100, 150, OpponentProfile.Balanced).FoldProbability;

        bigBet.ShouldBeGreaterThan(smallBet);
    }

    [Fact]
    public void ACallingStationFoldsLessThanATightOpponent()
    {
        double station = OpponentResponseModel
            .SplitFacingBet(Ranked, 100, 75, OpponentProfile.CallingStation).FoldProbability;
        double tight = OpponentResponseModel
            .SplitFacingBet(Ranked, 100, 75, OpponentProfile.Tight).FoldProbability;

        station.ShouldBeLessThan(tight);
    }

    [Fact]
    public void TheStrongestHandsAreTheOnesThatContinue()
    {
        RangeSplit split = OpponentResponseModel.SplitFacingBet(Ranked, 100, 100, OpponentProfile.Balanced);

        split.Raising.GetWeight(Ranked[0].Combo).ShouldBeGreaterThan(0);
        split.Folding.GetWeight(Ranked[^1].Combo).ShouldBeGreaterThan(0);
        split.Folding.GetWeight(Ranked[0].Combo).ShouldBe(0);
    }

    [Fact]
    public void ContinuingIsTheUnionOfCallingAndRaising()
    {
        RangeSplit split = OpponentResponseModel.SplitFacingBet(Ranked, 100, 60, OpponentProfile.Balanced);

        split.Continuing.TotalCombos.ShouldBe(
            split.Calling.TotalCombos + split.Raising.TotalCombos,
            1e-6);
    }

    [Fact]
    public void FacingNoBetAtAllTheWholeRangeContinues()
    {
        RangeSplit split = OpponentResponseModel.SplitFacingBet(Ranked, 100, 0, OpponentProfile.Balanced);

        split.FoldProbability.ShouldBe(0, 1e-9);
    }

    [Fact]
    public void ABettingRangeIsPolarisedBetweenValueAndBluff()
    {
        Core.Ranges.HandRange betting = OpponentResponseModel.BettingRange(Ranked, OpponentProfile.Balanced);

        betting.GetWeight(Ranked[0].Combo).ShouldBe(1, 1e-6);
        betting.GetWeight(Ranked[^1].Combo).ShouldBe(1, 1e-6);
        betting.GetWeight(Ranked[Ranked.Count * 3 / 4].Combo).ShouldBe(0, 1e-6);
        betting.TotalCombos.ShouldBe(
            Ranked.Count * (OpponentProfile.Balanced.BettingFraction + OpponentProfile.Balanced.BluffFraction),
            1e-6);
    }

    [Fact]
    public void AnEmptyRangeSplitsIntoNothing()
    {
        RangeSplit split = OpponentResponseModel.SplitFacingBet([], 100, 50, OpponentProfile.Balanced);

        split.FoldProbability.ShouldBe(1);
        split.Continuing.IsEmpty.ShouldBeTrue();
    }

    private static IReadOnlyList<RankedCombo> BuildRanked(int count)
    {
        List<RankedCombo> ranked = [];

        for (int index = 0; index < count; index++)
        {
            ranked.Add(new RankedCombo(HoleCards.FromIndex(index), 1, 1 - (index / (double)count)));
        }

        return ranked;
    }
}
