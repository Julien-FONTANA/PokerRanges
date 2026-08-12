using Microsoft.Extensions.Logging.Abstractions;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Equity;
using PokerRanges.Core.Evaluation;
using PokerRanges.Core.HeadToHead;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Preflop;
using PokerRanges.Core.Ranges;
using PokerRanges.Core.Table;
using Shouldly;

namespace PokerRanges.Core.Tests.HeadToHead;

public sealed class HeadToHeadCalculatorTests
{
    private readonly PotEngine _potEngine = new(NullLogger<PotEngine>.Instance);

    private readonly HeadToHeadCalculator _calculator = new(
        new EquityCalculator(new RankCountHandEvaluator(), NullLogger<EquityCalculator>.Instance),
        NullLogger<HeadToHeadCalculator>.Instance);

    [Fact]
    public async Task AnOpponentPinnedToOneHandNeverFolds()
    {
        HeadToHeadSpot spot = Spot(20, 20, Position.SmallBlind, HeadToHeadRole.Jamming);

        HeadToHeadResult result = await ComputeAsync(new HeadToHeadRequest
        {
            HeroRange = RangeNotationParser.Parse("AA"),
            VillainRange = Combo("KsKh"),
            VillainCards = HoleCards.Parse("KsKh"),
            Spot = spot,
        });

        result.VillainContinueFrequency.ShouldBe(1, 1e-9);
        result.Rationale.ShouldContain(HeadToHeadText.VillainPinnedToOneHand);

        // With no folds to collect, the jam is just the showdown minus what it risks.
        HeadToHeadActionEvaluation jam = result.Actions.Single(action => action.Kind == ChartActionKind.Jam);
        jam.ExpectedValue.ShouldBe((result.Hero.Equity * spot.ContestedPot) - spot.HeroRisk, 1e-9);

        result.Actions.Single(action => action.Kind == ChartActionKind.Fold).ExpectedValue.ShouldBe(0);
    }

    [Fact]
    public async Task TheVillainsCallingRangeSetsHowOftenHeFolds()
    {
        HeadToHeadResult result = await ComputeAsync(new HeadToHeadRequest
        {
            HeroRange = Combo("AsKs"),
            HeroCards = HoleCards.Parse("AsKs"),
            VillainRange = RangeNotationParser.Parse("QQ+"),
            Spot = Spot(20, 20, Position.SmallBlind, HeadToHeadRole.Jamming),
        });

        // Holding the ace and king of spades leaves him 1225 possible hands, of which QQ+ is
        // 6 + 3 + 3 = 12 once his kings and aces are blocked too.
        result.VillainContinueFrequency.ShouldBe(12.0 / 1225.0, 1e-9);
    }

    [Fact]
    public async Task TheBreakEvenFoldFrequencyMakesTheJamWorthExactlyZero()
    {
        HeadToHeadSpot spot = Spot(20, 20, Position.SmallBlind, HeadToHeadRole.Jamming);

        HeadToHeadResult result = await ComputeAsync(new HeadToHeadRequest
        {
            HeroRange = Combo("7d2c"),
            HeroCards = HoleCards.Parse("7d2c"),
            VillainRange = RangeNotationParser.Parse("AA"),
            Spot = spot,
        });

        double frequency = result.BreakEvenFoldFrequency.ShouldNotBeNull();
        frequency.ShouldBeInRange(0, 1);

        double surplus = (result.Hero.Equity * spot.ContestedPot) - spot.HeroRisk;
        double atThatFrequency = (frequency * spot.UncontestedPot) + ((1 - frequency) * surplus);

        atThatFrequency.ShouldBe(0, 1e-9);
    }

    /// <summary>
    /// The case a clamp would get backwards: past the point where the jam wins money even when
    /// always called, there is no fold frequency to reach at all.
    /// </summary>
    [Fact]
    public async Task AJamAheadOfEveryCallingHandNeedsNoFoldsAtAll()
    {
        HeadToHeadResult result = await ComputeAsync(new HeadToHeadRequest
        {
            HeroRange = Combo("AsAh"),
            HeroCards = HoleCards.Parse("AsAh"),
            VillainRange = RangeNotationParser.Parse("72o"),
            Spot = Spot(20, 20, Position.SmallBlind, HeadToHeadRole.Jamming),
        });

        result.BreakEvenFoldFrequency.ShouldBeNull();
        result.Rationale.ShouldContain(HeadToHeadText.JamProfitableWithoutAnyFold);
        result.Best.Kind.ShouldBe(ChartActionKind.Jam);
    }

    [Fact]
    public async Task FacingAJamTheCallIsPricedByThePotOdds()
    {
        HeadToHeadSpot spot = Spot(20, 30, Position.BigBlind, HeadToHeadRole.CallingAJam);

        HeadToHeadResult result = await ComputeAsync(new HeadToHeadRequest
        {
            HeroRange = Combo("AhKh"),
            HeroCards = HoleCards.Parse("AhKh"),
            VillainRange = RangeNotationParser.Parse("22+, A2s+, A2o+"),
            Spot = spot,
        });

        spot.BreakEvenEquityIfCalled.ShouldBe(0.45, 1e-9);

        HeadToHeadActionEvaluation call = result.Actions.Single(action => action.Kind == ChartActionKind.Call);
        call.ExpectedValue.ShouldBe((result.Hero.Equity * 40) - 18, 1e-9);

        // A jamming range is not an acceptance set, so no fold frequency is read off it.
        result.VillainContinueFrequency.ShouldBe(1, 1e-9);
        result.Rationale.ShouldContain(HeadToHeadText.CallingRangeIsTheVillainsJam);
    }

    [Fact]
    public async Task APlayerWhoCannotFoldIsNotOfferedAFold()
    {
        HeadToHeadSpot spot = Spot(2, 20, Position.BigBlind, HeadToHeadRole.CallingAJam, ante: 2);

        HeadToHeadResult result = await ComputeAsync(new HeadToHeadRequest
        {
            HeroRange = Combo("AhKh"),
            HeroCards = HoleCards.Parse("AhKh"),
            VillainRange = RangeNotationParser.Parse("22+, A2s+, A2o+"),
            Spot = spot,
        });

        result.Actions.ShouldHaveSingleItem();
        result.Best.Label.ShouldBe(HeadToHeadText.Showdown);
        result.Best.ExpectedValue.ShouldBe(result.Hero.Equity * spot.ContestedPot, 1e-9);
        result.Rationale.ShouldContain(HeadToHeadText.HeroCannotFold);
    }

    [Fact]
    public async Task AnAllInOpponentNeverFoldsHoweverNarrowHisRange()
    {
        HeadToHeadResult result = await ComputeAsync(new HeadToHeadRequest
        {
            HeroRange = Combo("7d2c"),
            HeroCards = HoleCards.Parse("7d2c"),
            VillainRange = RangeNotationParser.Parse("AA"),
            Spot = Spot(20, 2, Position.SmallBlind, HeadToHeadRole.Jamming, ante: 2),
        });

        result.VillainContinueFrequency.ShouldBe(1, 1e-9);
        result.Rationale.ShouldContain(HeadToHeadText.VillainCannotFold);
    }

    [Fact]
    public async Task ARangeTheBoardHasEmptiedIsNamedAsTheSideItBelongsTo()
    {
        HeadToHeadRequest request = new()
        {
            HeroRange = RangeNotationParser.Parse("AA"),
            VillainRange = RangeNotationParser.Parse("22+"),
            Board = TestCards.Parse("AsAhAd"),
            Spot = Spot(20, 20, Position.SmallBlind, HeadToHeadRole.Jamming),
        };

        HeadToHeadException failure = await Should.ThrowAsync<HeadToHeadException>(() => ComputeAsync(request));

        failure.Message.ShouldBe(HeadToHeadText.EmptyHeroRange);
    }

    /// <summary>
    /// Anchored on a published number, which is also the only thing that would catch the two sides
    /// being handed to the equity engine the wrong way round.
    /// </summary>
    [Fact]
    public async Task AceKingSuitedAgainstQueensLandsOnItsPublishedEquity()
    {
        HeadToHeadResult result = await ComputeAsync(new HeadToHeadRequest
        {
            HeroRange = Combo("AhKh"),
            HeroCards = HoleCards.Parse("AhKh"),
            VillainRange = Combo("QsQd"),
            VillainCards = HoleCards.Parse("QsQd"),
            Spot = Spot(20, 20, Position.SmallBlind, HeadToHeadRole.Jamming),
        });

        result.Hero.Equity.ShouldBe(0.460, 0.015);
        result.Villain.Equity.ShouldBe(0.540, 0.015);
    }

    [Fact]
    public async Task TheSameSpotAlwaysGivesTheSameAnswer()
    {
        HeadToHeadRequest request = new()
        {
            HeroRange = RangeNotationParser.Parse("77+, ATs+"),
            VillainRange = RangeNotationParser.Parse("22+, A2s+, KTo+"),
            Spot = Spot(24, 24, Position.SmallBlind, HeadToHeadRole.Jamming),
            Method = EquityMethod.MonteCarlo,
            MaximumSamples = 20_000,
        };

        HeadToHeadResult first = await ComputeAsync(request);
        HeadToHeadResult second = await ComputeAsync(request);

        second.Hero.Equity.ShouldBe(first.Hero.Equity, 1e-12);
        second.Best.ExpectedValue.ShouldBe(first.Best.ExpectedValue, 1e-12);
    }

    private static HandRange Combo(string text)
    {
        return new HandRangeBuilder().Set(HoleCards.Parse(text), 1).Build();
    }

    private Task<HeadToHeadResult> ComputeAsync(HeadToHeadRequest request)
    {
        // Sampling rather than enumeration: two known hands preflop sit just under the engine's
        // exhaustive budget, and these tests do not need that precision.
        return _calculator.ComputeAsync(
            request with { Method = EquityMethod.MonteCarlo, MaximumSamples = 20_000 },
            TestContext.Current.CancellationToken);
    }

    private HeadToHeadSpot Spot(
        double heroStack,
        double villainStack,
        Position heroSeat,
        HeadToHeadRole role,
        double ante = 0)
    {
        Position villainSeat = heroSeat == Position.SmallBlind ? Position.BigBlind : Position.SmallBlind;

        TableConfiguration table = new()
        {
            PlayerCount = 2,
            BigBlind = 2,
            StartingStacks = new Dictionary<Position, double>
            {
                [heroSeat] = heroStack,
                [villainSeat] = villainStack,
            },
            HeroPosition = heroSeat,
            AnteStyle = ante > 0 ? AnteStyle.BigBlindAnte : AnteStyle.None,
            AnteAmount = ante,
        };

        return HeadToHeadSpot.BetweenSeats(_potEngine, table, villainSeat, role);
    }
}
