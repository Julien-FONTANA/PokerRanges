using Microsoft.Extensions.Logging.Abstractions;
using PokerRanges.Core.Table;
using Shouldly;

namespace PokerRanges.Core.Tests.Table;

public sealed class PotEngineTests
{
    private readonly PotEngine _engine = new(NullLogger<PotEngine>.Instance);

    [Fact]
    public void TheBlindsAreInThePotBeforeAnyoneHasActed()
    {
        HandAnalysis analysis = _engine.Analyse(Hand(8, Position.UnderTheGun));

        analysis.Pot.ShouldBe(12);
        analysis.CurrentBet.ShouldBe(8);
        analysis.NextToAct.ShouldBe(Position.UnderTheGun);
        analysis.Hero.AmountToCall.ShouldBe(8);
        analysis.Hero.RemainingStack.ShouldBe(1000);
    }

    [Fact]
    public void ABigBlindAnteAddsOneAnteToThePotWhateverTheTableSize()
    {
        HandState state = Hand(8, Position.UnderTheGun) with
        {
            Table = Table(8, Position.UnderTheGun) with { AnteStyle = AnteStyle.BigBlindAnte, AnteAmount = 8 },
        };

        HandAnalysis analysis = _engine.Analyse(state);

        analysis.Pot.ShouldBe(20);
        analysis.For(Position.BigBlind).Committed.ShouldBe(16);
        analysis.For(Position.BigBlind).RemainingStack.ShouldBe(984);
    }

    [Fact]
    public void APerPlayerAnteIsPaidByEverySeat()
    {
        HandState state = Hand(6, Position.UnderTheGun) with
        {
            Table = Table(6, Position.UnderTheGun) with { AnteStyle = AnteStyle.PerPlayer, AnteAmount = 1 },
        };

        _engine.Analyse(state).Pot.ShouldBe(18);
    }

    [Fact]
    public void AnOpenRaiseLeavesTheBigBlindWithTheDifferenceToPay()
    {
        HandState state = Hand(6, Position.Button)
            .With(PlayerAction.Fold(Street.Preflop, Position.UnderTheGun))
            .With(PlayerAction.Fold(Street.Preflop, Position.HiJack))
            .With(PlayerAction.Fold(Street.Preflop, Position.CutOff))
            .With(PlayerAction.RaiseTo(Street.Preflop, Position.Button, 20));

        HandAnalysis analysis = _engine.Analyse(state);

        analysis.Pot.ShouldBe(32);
        analysis.NextToAct.ShouldBe(Position.SmallBlind);
        analysis.For(Position.BigBlind).AmountToCall.ShouldBe(12);
        analysis.For(Position.SmallBlind).AmountToCall.ShouldBe(16);
    }

    [Fact]
    public void TheBigBlindKeepsItsOptionWhenEveryoneLimps()
    {
        HandState state = Hand(3, Position.BigBlind)
            .With(PlayerAction.Call(Street.Preflop, Position.Button))
            .With(PlayerAction.Call(Street.Preflop, Position.SmallBlind));

        HandAnalysis analysis = _engine.Analyse(state);

        analysis.NextToAct.ShouldBe(Position.BigBlind);
        analysis.Hero.AmountToCall.ShouldBe(0);
    }

    [Fact]
    public void TheBettingRoundClosesOnceEveryoneHasAnswered()
    {
        HandState state = Hand(3, Position.BigBlind)
            .With(PlayerAction.Call(Street.Preflop, Position.Button))
            .With(PlayerAction.Call(Street.Preflop, Position.SmallBlind))
            .With(PlayerAction.Check(Street.Preflop, Position.BigBlind));

        _engine.Analyse(state).NextToAct.ShouldBeNull();
    }

    [Fact]
    public void ARaiseReopensTheActionForEveryoneWhoHadAlreadyCalled()
    {
        HandState state = Hand(6, Position.UnderTheGun)
            .With(PlayerAction.Call(Street.Preflop, Position.UnderTheGun))
            .With(PlayerAction.Call(Street.Preflop, Position.HiJack))
            .With(PlayerAction.RaiseTo(Street.Preflop, Position.CutOff, 32));

        HandAnalysis analysis = _engine.Analyse(state);

        analysis.NextToAct.ShouldBe(Position.Button);
        analysis.For(Position.UnderTheGun).AmountToCall.ShouldBe(24);
    }

    [Fact]
    public void TheFlopStartsANewBettingRoundWithNothingToCall()
    {
        HandAnalysis analysis = _engine.Analyse(SingleRaisedPotOnTheFlop());

        analysis.Street.ShouldBe(Street.Flop);
        analysis.Pot.ShouldBe(44);
        analysis.CurrentBet.ShouldBe(0);
        analysis.NextToAct.ShouldBe(Position.BigBlind);
        analysis.For(Position.BigBlind).AmountToCall.ShouldBe(0);
    }

    [Fact]
    public void TheOddsAndTheDefenceFrequencyFollowTheBetSize()
    {
        HandState state = SingleRaisedPotOnTheFlop()
            .With(PlayerAction.Check(Street.Flop, Position.BigBlind))
            .With(PlayerAction.BetTo(Street.Flop, Position.Button, 22));

        PotSnapshot facingTheBet = _engine.Analyse(state).For(Position.BigBlind);

        facingTheBet.Pot.ShouldBe(66);
        facingTheBet.AmountToCall.ShouldBe(22);
        facingTheBet.RequiredEquityToCall.ShouldBe(0.25, 1e-9);
        facingTheBet.MinimumDefenceFrequency.ShouldBe(2.0 / 3.0, 1e-9);
        facingTheBet.EffectiveStack.ShouldBe(958);
        facingTheBet.StackToPotRatio.ShouldBe(958.0 / 66.0, 1e-9);
    }

    [Fact]
    public void AShortStackCanOnlyCommitWhatItHasLeft()
    {
        Dictionary<Position, double> stacks = new()
        {
            [Position.SmallBlind] = 1000,
            [Position.BigBlind] = 120,
            [Position.Button] = 1000,
        };

        HandState state = new HandState
        {
            Table = new TableConfiguration
            {
                PlayerCount = 3,
                BigBlind = 8,
                StartingStacks = stacks,
                HeroPosition = Position.BigBlind,
            },
        }
            .With(PlayerAction.RaiseTo(Street.Preflop, Position.Button, 300))
            .With(PlayerAction.Fold(Street.Preflop, Position.SmallBlind))
            .With(PlayerAction.Call(Street.Preflop, Position.BigBlind));

        HandAnalysis analysis = _engine.Analyse(state);

        analysis.Hero.RemainingStack.ShouldBe(0);
        analysis.Hero.Committed.ShouldBe(120);
        analysis.Pot.ShouldBe(4 + 300 + 120);
    }

    [Fact]
    public void TheEffectiveStackIsCappedByTheShortestOfTheTwo()
    {
        Dictionary<Position, double> stacks = new()
        {
            [Position.SmallBlind] = 1000,
            [Position.BigBlind] = 1000,
            [Position.Button] = 200,
        };

        HandState state = new HandState
        {
            Table = new TableConfiguration
            {
                PlayerCount = 3,
                BigBlind = 8,
                StartingStacks = stacks,
                HeroPosition = Position.SmallBlind,
            },
        }.With(PlayerAction.Fold(Street.Preflop, Position.Button));

        _engine.Analyse(state).Hero.EffectiveStack.ShouldBe(992);
    }

    [Fact]
    public void CheckingWhileFacingABetIsRefused()
    {
        HandState state = Hand(3, Position.BigBlind)
            .With(PlayerAction.RaiseTo(Street.Preflop, Position.Button, 24))
            .With(PlayerAction.Check(Street.Preflop, Position.SmallBlind));

        Should.Throw<TableException>(() => _engine.Analyse(state));
    }

    [Fact]
    public void RaisingBelowTheCurrentBetIsRefused()
    {
        HandState state = Hand(3, Position.BigBlind)
            .With(PlayerAction.RaiseTo(Street.Preflop, Position.Button, 24))
            .With(PlayerAction.RaiseTo(Street.Preflop, Position.SmallBlind, 16));

        Should.Throw<TableException>(() => _engine.Analyse(state));
    }

    [Fact]
    public void ActingAfterFoldingIsRefused()
    {
        HandState state = Hand(3, Position.BigBlind)
            .With(PlayerAction.Fold(Street.Preflop, Position.Button))
            .With(PlayerAction.Call(Street.Preflop, Position.Button));

        Should.Throw<TableException>(() => _engine.Analyse(state));
    }

    [Fact]
    public void AHeroSeatedAtAPositionThatDoesNotExistIsRefused()
    {
        Should.Throw<TableException>(() => _engine.Analyse(Hand(4, Position.UnderTheGun)));
    }

    private static HandState SingleRaisedPotOnTheFlop()
    {
        return Hand(6, Position.Button)
            .With(PlayerAction.Fold(Street.Preflop, Position.UnderTheGun))
            .With(PlayerAction.Fold(Street.Preflop, Position.HiJack))
            .With(PlayerAction.Fold(Street.Preflop, Position.CutOff))
            .With(PlayerAction.RaiseTo(Street.Preflop, Position.Button, 20))
            .With(PlayerAction.Fold(Street.Preflop, Position.SmallBlind))
            .With(PlayerAction.Call(Street.Preflop, Position.BigBlind))
            .WithBoard(TestCards.Parse("Kh8d3c"));
    }

    private static TableConfiguration Table(int playerCount, Position hero)
    {
        return TableConfiguration.Uniform(playerCount, 8, 1000, hero);
    }

    private static HandState Hand(int playerCount, Position hero)
    {
        return new HandState { Table = Table(playerCount, hero) };
    }
}
