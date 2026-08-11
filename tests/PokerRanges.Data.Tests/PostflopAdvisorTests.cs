using Microsoft.Extensions.Logging.Abstractions;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Equity;
using PokerRanges.Core.Evaluation;
using PokerRanges.Core.Postflop;
using PokerRanges.Core.Preflop;
using PokerRanges.Core.Table;
using PokerRanges.Data;
using Shouldly;

namespace PokerRanges.Data.Tests;

/// <summary>
/// Scénarios de référence du moteur postflop. Ils ne vérifient pas que le conseil est « le bon
/// coup » au sens d'un solveur, mais que le modèle se comporte comme il le promet : l'équité est
/// mesurée contre la range qui paie, les mains fortes misent, les mains mortes ne suivent pas.
/// </summary>
public sealed class PostflopAdvisorTests
{
    private static readonly PostflopBudget TestBudget = PostflopBudget.Full with
    {
        RankingSamplesPerCombo = 80,
        EquitySamples = 6_000,
    };

    private readonly EvPostflopAdvisor _advisor;

    public PostflopAdvisorTests()
    {
        RankCountHandEvaluator evaluator = new();
        PotEngine potEngine = new(NullLogger<PotEngine>.Instance);
        JsonPreflopChartRepository charts = new(
            PreflopChartRepositoryOptions.EmbeddedOnly,
            NullLogger<JsonPreflopChartRepository>.Instance);
        RangeStrengthRanker ranker = new(evaluator, PostflopOptions.Default, NullLogger<RangeStrengthRanker>.Instance);

        _advisor = new EvPostflopAdvisor(
            potEngine,
            new RangeAssigner(charts, potEngine, ranker, PreflopAdvisorOptions.Default, NullLogger<RangeAssigner>.Instance),
            ranker,
            new MadeHandClassifier(evaluator),
            new EquityCalculator(evaluator, NullLogger<EquityCalculator>.Instance),
            PostflopOptions.Default,
            NullLogger<EvPostflopAdvisor>.Instance);
    }

    [Fact]
    public async Task ASetBetsForValueWhenItIsCheckedTo()
    {
        PostflopAdvice advice = await Advise(SingleRaisedPot("7h7d", "7s2c3d")
            .With(PlayerAction.Check(Street.Flop, Position.BigBlind)));

        advice.HeroHand.Tier.ShouldBe(MadeHandTier.Set);
        advice.Best.Kind.ShouldBe(PostflopActionKind.Bet);
        advice.Best.ExpectedValue.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task AirFacingAPotSizedBetIsNotWorthCalling()
    {
        PostflopAdvice advice = await Advise(SingleRaisedPot("QhJd", "Ks8d3c")
            .With(PlayerAction.BetTo(Street.Flop, Position.BigBlind, 40)));

        ActionEvaluation call = advice.Candidates.Single(entry => entry.Kind == PostflopActionKind.Call);

        call.ExpectedValue.ShouldBeLessThan(0);
        advice.Best.Kind.ShouldNotBe(PostflopActionKind.Call);
    }

    [Fact]
    public async Task ANutFlushDrawCallsASmallBet()
    {
        PostflopAdvice advice = await Advise(SingleRaisedPot("AsQs", "Ks8s3c")
            .With(PlayerAction.BetTo(Street.Flop, Position.BigBlind, 10)));

        advice.HeroHand.HasFlushDraw.ShouldBeTrue();
        advice.Candidates.Single(entry => entry.Kind == PostflopActionKind.Call)
            .ExpectedValue.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ASetNeverFoldsFacingABet()
    {
        PostflopAdvice advice = await Advise(SingleRaisedPot("3h3d", "Ks8d3c")
            .With(PlayerAction.BetTo(Street.Flop, Position.BigBlind, 30)));

        advice.Best.Kind.ShouldBeOneOf(PostflopActionKind.Call, PostflopActionKind.Raise);
        advice.Candidates.Single(entry => entry.Kind == PostflopActionKind.Call)
            .ExpectedValue.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// La promesse centrale du moteur : une mise n'est payée que par le haut de la range adverse,
    /// donc l'équité une fois payé est plus basse que l'équité contre la range entière.
    /// </summary>
    [Fact]
    public async Task TheEquityOnceCalledIsLowerThanAgainstTheWholeRange()
    {
        PostflopAdvice advice = await Advise(SingleRaisedPot("Kh9d", "Ks8d3c")
            .With(PlayerAction.Check(Street.Flop, Position.BigBlind)));

        double againstEverything = advice.Candidates
            .Single(entry => entry.Kind == PostflopActionKind.Check).Equity;
        double whenCalled = advice.Candidates
            .Where(entry => entry.Kind == PostflopActionKind.Bet)
            .Max(entry => entry.Equity);

        whenCalled.ShouldBeLessThan(againstEverything);
    }

    [Fact]
    public async Task ABiggerBetIsFoldedToMoreOften()
    {
        PostflopAdvice advice = await Advise(SingleRaisedPot("Kh9d", "Ks8d3c")
            .With(PlayerAction.Check(Street.Flop, Position.BigBlind)));

        List<ActionEvaluation> bets =
        [
            .. advice.Candidates.Where(entry => entry.Kind == PostflopActionKind.Bet).OrderBy(entry => entry.Amount),
        ];

        bets.Count.ShouldBeGreaterThan(1);
        bets[^1].FoldProbability.ShouldBeGreaterThan(bets[0].FoldProbability);
    }

    [Fact]
    public async Task FoldingIsOnlyOfferedWhenThereIsSomethingToPay()
    {
        PostflopAdvice checkedTo = await Advise(SingleRaisedPot("Kh9d", "Ks8d3c")
            .With(PlayerAction.Check(Street.Flop, Position.BigBlind)));
        PostflopAdvice facingBet = await Advise(SingleRaisedPot("Kh9d", "Ks8d3c")
            .With(PlayerAction.BetTo(Street.Flop, Position.BigBlind, 20)));

        checkedTo.Candidates.ShouldNotContain(entry => entry.Kind == PostflopActionKind.Fold);
        checkedTo.Candidates.ShouldContain(entry => entry.Kind == PostflopActionKind.Check);

        facingBet.Candidates.Single(entry => entry.Kind == PostflopActionKind.Fold).ExpectedValue.ShouldBe(0);
        facingBet.Candidates.ShouldNotContain(entry => entry.Kind == PostflopActionKind.Check);
    }

    [Fact]
    public async Task TheOpponentRangeIsNarrowedAndTheReasoningIsKept()
    {
        PostflopAdvice advice = await Advise(SingleRaisedPot("Kh9d", "Ks8d3c")
            .With(PlayerAction.BetTo(Street.Flop, Position.BigBlind, 20)));

        OpponentRange opponent = advice.Opponents.Single();

        opponent.Position.ShouldBe(Position.BigBlind);
        opponent.Combos.ShouldBeGreaterThan(0);
        opponent.Combos.ShouldBeLessThan(1326);
        opponent.Story.ShouldContain(line => line.Contains("called preflop", StringComparison.Ordinal));
        opponent.Story.ShouldContain(line => line.Contains("Flop", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheSameSituationAlwaysGivesTheSameAdvice()
    {
        HandState state = SingleRaisedPot("Kh9d", "Ks8d3c")
            .With(PlayerAction.Check(Street.Flop, Position.BigBlind));

        PostflopAdvice first = await Advise(state);
        PostflopAdvice second = await Advise(state);

        second.Best.Label.ShouldBe(first.Best.Label);
        second.Best.ExpectedValue.ShouldBe(first.Best.ExpectedValue);
    }

    [Fact]
    public async Task AMultiwayPotIsFlaggedAsSuch()
    {
        HandState state = ThreeWayPot("Kh9d", "Ks8d3c");

        PostflopAdvice advice = await Advise(state);

        advice.IsHeadsUp.ShouldBeFalse();
        advice.Opponents.Count.ShouldBe(2);
    }

    [Fact]
    public async Task TheReasoningNamesTheHandTheBoardAndTheOpponentModel()
    {
        PostflopAdvice advice = await Advise(SingleRaisedPot("Kh9d", "Ks8d3c")
            .With(PlayerAction.Check(Street.Flop, Position.BigBlind)));

        advice.Rationale.ShouldContain(line => line.Contains("top pair", StringComparison.Ordinal));
        advice.Rationale.ShouldContain(line => line.Contains("high board", StringComparison.Ordinal));
        advice.Rationale.ShouldContain(line => line.Contains("Balanced", StringComparison.Ordinal));
        advice.Rationale.ShouldContain(line => line.Contains("SPR", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ARiverDecisionUsesTheCompleteBoard()
    {
        HandState state = SingleRaisedPot("Kh9d", "Ks8d3c")
            .With(PlayerAction.Check(Street.Flop, Position.BigBlind))
            .With(PlayerAction.Check(Street.Flop, Position.Button))
            .WithBoard(TestCards.Parse("Ks8d3c2h7s"))
            .With(PlayerAction.Check(Street.Turn, Position.BigBlind))
            .With(PlayerAction.Check(Street.Turn, Position.Button))
            .With(PlayerAction.Check(Street.River, Position.BigBlind));

        PostflopAdvice advice = await Advise(state);

        advice.HeroHand.Outs.ShouldBe(0);
        advice.Board.CardCount.ShouldBe(5);
        advice.Best.ExpectedValue.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task AdviceWithoutHeroCardsIsRefused()
    {
        HandState state = SingleRaisedPot("Kh9d", "Ks8d3c") with { HeroCards = null };

        await Should.ThrowAsync<PostflopAdviceException>(() => Advise(state));
    }

    [Fact]
    public async Task AdviceBeforeTheFlopIsRefused()
    {
        HandState state = SingleRaisedPot("Kh9d", "Ks8d3c") with { Board = [] };

        await Should.ThrowAsync<PostflopAdviceException>(() => Advise(state));
    }

    /// <summary>
    /// L'échange que le mode compact fait à la table : moins de tirages, donc une réponse plus
    /// rapide, donc un intervalle de confiance plus large. Si le budget réduit rendait la même
    /// précision, c'est le budget complet qui gaspillerait.
    /// </summary>
    [Fact]
    public async Task TheSmallBudgetBuysSpeedWithPrecisionAndSaysSo()
    {
        HandState state = SingleRaisedPot("Kh9d", "Ks8d3c")
            .With(PlayerAction.Check(Street.Flop, Position.BigBlind));

        PostflopAdvice fast = await Advise(state, PostflopBudget.Fast);
        PostflopAdvice full = await Advise(state, PostflopBudget.Full);

        fast.EquityStandardError.ShouldBeGreaterThan(full.EquityStandardError);
        fast.Budget.Name.ShouldBe("Fast");
        fast.DescribePrecision().ShouldContain("Fast");
    }

    /// <summary>
    /// Une précision plus basse n'a d'intérêt que si elle mène au même endroit : sur un spot franc,
    /// les deux budgets doivent recommander la même action.
    /// </summary>
    [Fact]
    public async Task BothBudgetsAgreeOnAClearCutSpot()
    {
        HandState state = SingleRaisedPot("7h7d", "7s2c3d")
            .With(PlayerAction.Check(Street.Flop, Position.BigBlind));

        PostflopAdvice fast = await Advise(state, PostflopBudget.Fast);
        PostflopAdvice full = await Advise(state, PostflopBudget.Full);

        fast.Best.Kind.ShouldBe(full.Best.Kind);
    }

    [Fact]
    public async Task TheAdviceReportsHowLongItTook()
    {
        PostflopAdvice advice = await Advise(SingleRaisedPot("Kh9d", "Ks8d3c")
            .With(PlayerAction.Check(Street.Flop, Position.BigBlind)));

        advice.Duration.ShouldBeGreaterThan(TimeSpan.Zero);
        advice.DescribePrecision().ShouldContain("ms");
    }

    private Task<PostflopAdvice> Advise(HandState state)
    {
        return Advise(state, TestBudget);
    }

    private Task<PostflopAdvice> Advise(HandState state, PostflopBudget budget)
    {
        return _advisor.AdviseAsync(state, OpponentProfile.Balanced, budget, TestContext.Current.CancellationToken);
    }

    /// <summary>Bouton ouvre à 18, grosse blinde suit : pot de 40 jetons à 100bb de profondeur.</summary>
    private static HandState SingleRaisedPot(string heroCards, string board)
    {
        return new HandState
        {
            Table = TableConfiguration.Uniform(6, 8, 800, Position.Button),
            HeroCards = HoleCards.Parse(heroCards),
        }
            .With(PlayerAction.Fold(Street.Preflop, Position.UnderTheGun))
            .With(PlayerAction.Fold(Street.Preflop, Position.HiJack))
            .With(PlayerAction.Fold(Street.Preflop, Position.CutOff))
            .With(PlayerAction.RaiseTo(Street.Preflop, Position.Button, 18))
            .With(PlayerAction.Fold(Street.Preflop, Position.SmallBlind))
            .With(PlayerAction.Call(Street.Preflop, Position.BigBlind))
            .WithBoard(TestCards.Parse(board));
    }

    private static HandState ThreeWayPot(string heroCards, string board)
    {
        return new HandState
        {
            Table = TableConfiguration.Uniform(6, 8, 800, Position.Button),
            HeroCards = HoleCards.Parse(heroCards),
        }
            .With(PlayerAction.Fold(Street.Preflop, Position.UnderTheGun))
            .With(PlayerAction.Fold(Street.Preflop, Position.HiJack))
            .With(PlayerAction.RaiseTo(Street.Preflop, Position.CutOff, 18))
            .With(PlayerAction.Call(Street.Preflop, Position.Button))
            .With(PlayerAction.Fold(Street.Preflop, Position.SmallBlind))
            .With(PlayerAction.Call(Street.Preflop, Position.BigBlind))
            .WithBoard(TestCards.Parse(board))
            .With(PlayerAction.Check(Street.Flop, Position.BigBlind))
            .With(PlayerAction.Check(Street.Flop, Position.CutOff));
    }
}
