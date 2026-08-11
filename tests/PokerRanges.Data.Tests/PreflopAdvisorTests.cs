using Microsoft.Extensions.Logging.Abstractions;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Preflop;
using PokerRanges.Core.Table;
using PokerRanges.Data;
using Shouldly;

namespace PokerRanges.Data.Tests;

public sealed class PreflopAdvisorTests
{
    private readonly PreflopAdvisor _advisor;

    public PreflopAdvisorTests()
    {
        JsonPreflopChartRepository charts = new(
            PreflopChartRepositoryOptions.EmbeddedOnly,
            NullLogger<JsonPreflopChartRepository>.Instance);

        _advisor = new PreflopAdvisor(
            charts,
            new PotEngine(NullLogger<PotEngine>.Instance),
            PreflopAdvisorOptions.Default,
            NullLogger<PreflopAdvisor>.Instance);
    }

    [Fact]
    public void AShortStackedButtonJamsItsSuitedAce()
    {
        PreflopAdvice advice = _advisor.Advise(Hand(3, Position.Button, 96, "As2s"));

        advice.Situation.Context.ShouldBe(PreflopContext.Jam);
        advice.Situation.DepthInBigBlinds.ShouldBe(12);
        advice.Recommendation.Kind.ShouldBe(ChartActionKind.Jam);
    }

    [Fact]
    public void ADeepCutOffOpensItsBroadway()
    {
        PreflopAdvice advice = _advisor.Advise(Hand(8, Position.CutOff, 1000, "AhQd"));

        advice.Situation.Context.ShouldBe(PreflopContext.RaiseFirstIn);
        advice.Situation.PlayersLeftToAct.ShouldBe(3);
        advice.Situation.DepthInBigBlinds.ShouldBe(125);
        advice.Recommendation.Kind.ShouldBe(ChartActionKind.Raise);
        advice.Recommendation.SizeInBigBlinds.ShouldBe(2.2);
    }

    [Fact]
    public void TheWorstHandUnderTheGunIsFolded()
    {
        PreflopAdvice advice = _advisor.Advise(Hand(8, Position.UnderTheGun, 1000, "7h2c"));

        advice.Recommendation.Kind.ShouldBe(ChartActionKind.Fold);
        advice.Recommendation.Frequency.ShouldBe(1);
    }

    [Fact]
    public void TheChartActuallyUsedIsAlwaysReported()
    {
        PreflopAdvice advice = _advisor.Advise(Hand(8, Position.CutOff, 1000, "AhQd"));

        advice.Chart.IsExactMatch.ShouldBeFalse();
        advice.Chart.Describe().ShouldContain("100");
        advice.Rationale.ShouldContain(line => line.Contains("125", StringComparison.Ordinal));
    }

    [Fact]
    public void TheBigBlindDefendsAgainstAnOpen()
    {
        HandState state = Hand(6, Position.BigBlind, 800, "Ks9s")
            .With(PlayerAction.Fold(Street.Preflop, Position.UnderTheGun))
            .With(PlayerAction.Fold(Street.Preflop, Position.HiJack))
            .With(PlayerAction.Fold(Street.Preflop, Position.CutOff))
            .With(PlayerAction.RaiseTo(Street.Preflop, Position.Button, 18))
            .With(PlayerAction.Fold(Street.Preflop, Position.SmallBlind));

        PreflopAdvice advice = _advisor.Advise(state);

        advice.Situation.Context.ShouldBe(PreflopContext.VersusOpen);
        advice.Situation.Relation.ShouldBe(FacingRelation.BigBlind);
        advice.Situation.Aggressor.ShouldBe(Position.Button);
        advice.Recommendation.Kind.ShouldBe(ChartActionKind.Call);
        advice.Rationale.ShouldContain(line => line.Contains("equity is needed", StringComparison.Ordinal));
    }

    [Fact]
    public void TheBigBlindThreeBetsItsBestHands()
    {
        HandState state = Hand(6, Position.BigBlind, 800, "AsKh")
            .With(PlayerAction.Fold(Street.Preflop, Position.UnderTheGun))
            .With(PlayerAction.Fold(Street.Preflop, Position.HiJack))
            .With(PlayerAction.Fold(Street.Preflop, Position.CutOff))
            .With(PlayerAction.RaiseTo(Street.Preflop, Position.Button, 18))
            .With(PlayerAction.Fold(Street.Preflop, Position.SmallBlind));

        PreflopAdvice advice = _advisor.Advise(state);

        advice.Recommendation.Kind.ShouldBe(ChartActionKind.Raise);
        advice.Recommendation.SizeInBigBlinds.ShouldBe(11);
    }

    [Fact]
    public void AnOpenerFacingAThreeBetIsRecognisedAsSuch()
    {
        HandState state = Hand(6, Position.CutOff, 800, "AsKh")
            .With(PlayerAction.Fold(Street.Preflop, Position.UnderTheGun))
            .With(PlayerAction.Fold(Street.Preflop, Position.HiJack))
            .With(PlayerAction.RaiseTo(Street.Preflop, Position.CutOff, 18))
            .With(PlayerAction.RaiseTo(Street.Preflop, Position.Button, 54))
            .With(PlayerAction.Fold(Street.Preflop, Position.SmallBlind))
            .With(PlayerAction.Fold(Street.Preflop, Position.BigBlind));

        PreflopAdvice advice = _advisor.Advise(state);

        advice.Situation.Context.ShouldBe(PreflopContext.VersusThreeBet);
        advice.Situation.Aggressor.ShouldBe(Position.Button);
        advice.Chart.Adjustments.ShouldContain(line => line.Contains("caution", StringComparison.Ordinal));
    }

    [Fact]
    public void ASqueezeSpotIsRecognisedWhenSomeoneHasAlreadyCalled()
    {
        HandState state = Hand(6, Position.BigBlind, 800, "AsKh")
            .With(PlayerAction.Fold(Street.Preflop, Position.UnderTheGun))
            .With(PlayerAction.RaiseTo(Street.Preflop, Position.HiJack, 18))
            .With(PlayerAction.Call(Street.Preflop, Position.CutOff))
            .With(PlayerAction.Fold(Street.Preflop, Position.Button))
            .With(PlayerAction.Fold(Street.Preflop, Position.SmallBlind));

        _advisor.Advise(state).Situation.Context.ShouldBe(PreflopContext.Squeeze);
    }

    [Fact]
    public void ALimpedPotIsRecognised()
    {
        HandState state = Hand(6, Position.Button, 800, "AsKh")
            .With(PlayerAction.Call(Street.Preflop, Position.UnderTheGun))
            .With(PlayerAction.Fold(Street.Preflop, Position.HiJack))
            .With(PlayerAction.Fold(Street.Preflop, Position.CutOff));

        PreflopSituation situation = _advisor.Advise(state).Situation;

        situation.Context.ShouldBe(PreflopContext.VersusLimp);
        situation.Limpers.ShouldBe(1);
    }

    [Fact]
    public void TheChartCanBeConsultedWithoutKnowingTheHeroHand()
    {
        HandState state = Hand(8, Position.Button, 1000, heroCards: null);

        ChartResolution resolution = _advisor.ResolveChart(state);

        resolution.Chart.Context.ShouldBe(PreflopContext.RaiseFirstIn);
        resolution.Strategy.RangeOf(ChartActionKind.Raise).PercentOfAllHands.ShouldBeInRange(35, 50);
    }

    [Fact]
    public void AdviceWithoutTheHeroHandIsRefused()
    {
        HandState state = Hand(8, Position.Button, 1000, heroCards: null);

        Should.Throw<PreflopChartException>(() => _advisor.Advise(state));
    }

    private static HandState Hand(int playerCount, Position hero, double stack, string? heroCards)
    {
        return new HandState
        {
            Table = TableConfiguration.Uniform(playerCount, 8, stack, hero),
            HeroCards = heroCards is null ? null : HoleCards.Parse(heroCards),
        };
    }
}
