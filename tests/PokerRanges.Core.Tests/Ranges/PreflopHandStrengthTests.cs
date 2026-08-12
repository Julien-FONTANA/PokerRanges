using PokerRanges.Core.Cards;
using PokerRanges.Core.Ranges;
using Shouldly;

namespace PokerRanges.Core.Tests.Ranges;

public sealed class PreflopHandStrengthTests
{
    private readonly PreflopHandStrength _strength = new();

    [Fact]
    public void EveryStartingHandIsRankedExactlyOnce()
    {
        _strength.Ordered.Count.ShouldBe(HandClass.Count);
        _strength.Ordered.Select(ranked => ranked.HandClass).Distinct().Count().ShouldBe(HandClass.Count);
    }

    [Fact]
    public void AcesLeadTheOrderingAndThreeTwoOffsuitClosesIt()
    {
        _strength.Ordered[0].HandClass.ShouldBe(HandClass.Parse("AA"));
        _strength.Ordered[^1].HandClass.ShouldBe(HandClass.Parse("32o"));
    }

    [Theory]
    [InlineData("AA", 0.852)]
    [InlineData("AKs", 0.670)]
    [InlineData("AKo", 0.652)]
    [InlineData("77", 0.662)]
    [InlineData("22", 0.503)]
    [InlineData("JTs", 0.576)]
    [InlineData("32o", 0.323)]
    public void TheMeasuredEquitiesLandOnTheirPublishedValues(string notation, double published)
    {
        HandClass handClass = HandClass.Parse(notation);

        RankedHandClass ranked = _strength.Ordered.Single(entry => entry.HandClass == handClass);

        ranked.EquityAgainstRandomHand.ShouldBe(published, 0.005);
    }

    [Fact]
    public void ASuitedHandAlwaysOutranksItsOffsuitTwin()
    {
        Dictionary<HandClass, int> rank = RankByHandClass();

        foreach (HandClass suited in HandClass.All.Where(handClass => handClass.Shape == HandShape.Suited))
        {
            HandClass offsuit = HandClass.Offsuit(suited.High, suited.Low);

            rank[suited].ShouldBeLessThan(rank[offsuit], $"{suited} should outrank {offsuit}");
        }
    }

    [Fact]
    public void EveryPairOutranksThePairBelowIt()
    {
        Dictionary<HandClass, int> rank = RankByHandClass();

        for (int low = (int)Rank.Two; low < (int)Rank.Ace; low++)
        {
            HandClass smaller = HandClass.Pair((Rank)low);
            HandClass larger = HandClass.Pair((Rank)(low + 1));

            rank[larger].ShouldBeLessThan(rank[smaller], $"{larger} should outrank {smaller}");
        }
    }

    /// <summary>
    /// Kings and queens only: an ace's kicker is not monotone and should not be asserted to be. A5
    /// makes the wheel where A6 makes nothing, which puts the two within a rounding error of each
    /// other — a real property of the game, not a flaw in the measurement.
    /// </summary>
    [Theory]
    [InlineData(Rank.King, HandShape.Suited)]
    [InlineData(Rank.King, HandShape.Offsuit)]
    [InlineData(Rank.Queen, HandShape.Suited)]
    [InlineData(Rank.Queen, HandShape.Offsuit)]
    public void AHigherKickerRanksHigher(Rank high, HandShape shape)
    {
        Dictionary<HandClass, int> rank = RankByHandClass();

        for (int low = (int)Rank.Two; low < (int)high - 1; low++)
        {
            HandClass weaker = new(high, (Rank)low, shape);
            HandClass stronger = new(high, (Rank)(low + 1), shape);

            rank[stronger].ShouldBeLessThan(rank[weaker], $"{stronger} should outrank {weaker}");
        }
    }

    [Fact]
    public void TheWholeGridIsEveryCombo()
    {
        _strength.TopPercent(100).TotalCombos.ShouldBe(HoleCards.Count, 1e-9);
    }

    [Fact]
    public void NothingIsSelectedAtZeroPercent()
    {
        _strength.TopPercent(0).IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void TheTopTenPercentHoldsATenthOfEveryCombo()
    {
        _strength.TopPercent(10).TotalCombos.ShouldBe(132.6, 1e-9);
    }

    [Fact]
    public void TheTopOfTheRangeIsTheStrongestHands()
    {
        HandRange top = _strength.TopPercent(2);

        top.FrequencyOf(HandClass.Parse("AA")).ShouldBe(1, 1e-9);
        top.FrequencyOf(HandClass.Parse("KK")).ShouldBe(1, 1e-9);
        top.FrequencyOf(HandClass.Parse("72o")).ShouldBe(0, 1e-9);
    }

    /// <summary>
    /// 4% of 1326 is 53.04 combos. The seven pairs down to 77 plus AKs account for 52 of them, so
    /// AQs straddles the cut-off and takes the remaining 1.04 spread over its four combos.
    /// </summary>
    [Fact]
    public void TheHandStraddlingTheCutOffIsThinnedAcrossAllOfItsCombos()
    {
        HandRange top = _strength.TopPercent(4);

        top.TotalCombos.ShouldBe(53.04, 1e-9);

        HandClass boundary = HandClass.Parse("AQs");
        foreach (HoleCards combo in boundary.Combos())
        {
            top.GetWeight(combo).ShouldBe(0.26, 1e-9);
        }

        // A uniform cell keeps the notation compact rather than degenerating into a combo list.
        top.ToString().ShouldContain("AQs:0.26");
    }

    [Fact]
    public void AWiderSliceContainsEveryHandOfANarrowerOne()
    {
        HandRange narrow = _strength.TopPercent(15);
        HandRange wide = _strength.TopPercent(30);

        foreach (WeightedCombo combo in narrow.EnumerateCombos())
        {
            wide.GetWeight(combo.Combo).ShouldBeGreaterThanOrEqualTo(combo.Weight - 1e-9);
        }
    }

    private Dictionary<HandClass, int> RankByHandClass()
    {
        Dictionary<HandClass, int> rank = [];
        for (int index = 0; index < _strength.Ordered.Count; index++)
        {
            rank[_strength.Ordered[index].HandClass] = index;
        }

        return rank;
    }
}
