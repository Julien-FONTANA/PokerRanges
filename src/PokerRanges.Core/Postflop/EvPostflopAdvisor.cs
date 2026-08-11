using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Equity;
using PokerRanges.Core.Evaluation;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Ranges;
using PokerRanges.Core.Table;

namespace PokerRanges.Core.Postflop;

/// <summary>
/// Choisit l'action de plus forte espérance parmi un jeu de tailles, sous un modèle de réponse
/// adverse explicite. Le point qui compte : l'équité est mesurée contre la sous-range qui paie,
/// pas contre la range de départ — c'est là que les outils naïfs se trompent.
/// En tête-à-tête la re-relance adverse est modélisée ; à plus de deux joueurs on se limite à
/// « tout le monde passe » ou « au moins un continue », ce que l'avis signale.
/// </summary>
public sealed class EvPostflopAdvisor : IPostflopAdvisor
{
    private readonly IPotEngine _potEngine;
    private readonly IRangeAssigner _assigner;
    private readonly IRangeStrengthRanker _ranker;
    private readonly IMadeHandClassifier _classifier;
    private readonly IEquityCalculator _equity;
    private readonly PostflopOptions _options;
    private readonly ILogger<EvPostflopAdvisor> _logger;

    public EvPostflopAdvisor(
        IPotEngine potEngine,
        IRangeAssigner assigner,
        IRangeStrengthRanker ranker,
        IMadeHandClassifier classifier,
        IEquityCalculator equity,
        PostflopOptions options,
        ILogger<EvPostflopAdvisor> logger)
    {
        _potEngine = potEngine;
        _assigner = assigner;
        _ranker = ranker;
        _classifier = classifier;
        _equity = equity;
        _options = options;
        _logger = logger;
    }

    public async Task<PostflopAdvice> AdviseAsync(
        HandState state,
        OpponentProfile profile,
        PostflopBudget budget,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(budget);

        long startedAt = Stopwatch.GetTimestamp();

        if (state.HeroCards is not HoleCards hero)
        {
            throw new PostflopAdviceException(PostflopText.HeroCardsRequired);
        }

        if (state.Board.Count < 3)
        {
            throw new PostflopAdviceException(PostflopText.FlopRequired(state.Board.Count));
        }

        HandAnalysis analysis = _potEngine.Analyse(state);
        PotSnapshot pot = analysis.Hero;

        IReadOnlyList<OpponentRange> opponents = _assigner.Assign(state, profile, budget, cancellationToken);
        if (opponents.Count == 0)
        {
            throw new PostflopAdviceException(PostflopText.NoOpponentLeft);
        }

        Card[] board = [.. state.Board];
        HandFeatures features = _classifier.Classify(hero, board);
        BoardTexture texture = BoardTexture.Read(board);

        // Le classement d'une range ne dépend pas de la taille envisagée : on le calcule une fois
        // pour toutes, sinon chaque taille de mise relancerait le même tirage Monte-Carlo.
        IReadOnlyList<IReadOnlyList<RankedCombo>> rankings =
        [
            .. opponents.Select(opponent => _ranker.Rank(
                opponent.Range,
                board,
                [hero.First, hero.Second],
                budget,
                cancellationToken)),
        ];

        List<ActionEvaluation> candidates = pot.IsFacingABet
            ? await EvaluateFacingBetAsync(state, hero, pot, opponents, rankings, features, profile, budget, cancellationToken).ConfigureAwait(false)
            : await EvaluateUncontestedAsync(state, hero, pot, opponents, rankings, analysis, profile, budget, cancellationToken).ConfigureAwait(false);

        candidates.Sort(static (left, right) => right.ExpectedValue.CompareTo(left.ExpectedValue));

        ActionEvaluation best = candidates[0];
        bool isClose = candidates.Count > 1
            && best.ExpectedValue - candidates[1].ExpectedValue
               < _options.CloseCallThresholdAsPotFraction * pot.Pot;

        double standardError = 0;
        foreach (ActionEvaluation candidate in candidates)
        {
            standardError = Math.Max(standardError, candidate.EquityStandardError);
        }

        TimeSpan duration = Stopwatch.GetElapsedTime(startedAt);

        _logger.LogInformation(
            "Conseil postflop : {Action} (EV {ExpectedValue:0.##}) avec {Hand} sur {Board} — budget {Budget} en {Elapsed:0} ms",
            best.Label,
            best.ExpectedValue,
            features.Describe(),
            string.Join(string.Empty, board),
            budget.Name,
            duration.TotalMilliseconds);

        return new PostflopAdvice
        {
            Best = best,
            Candidates = candidates,
            HeroHand = features,
            Board = texture,
            Opponents = opponents,
            Pot = pot,
            IsClose = isClose,
            IsHeadsUp = opponents.Count == 1,
            Budget = budget,
            EquityStandardError = standardError,
            Duration = duration,
            Rationale = BuildRationale(pot, features, texture, opponents, best, isClose, profile, state.Board.Count),
        };
    }

    private async Task<List<ActionEvaluation>> EvaluateFacingBetAsync(
        HandState state,
        HoleCards hero,
        PotSnapshot pot,
        IReadOnlyList<OpponentRange> opponents,
        IReadOnlyList<IReadOnlyList<RankedCombo>> rankings,
        HandFeatures features,
        OpponentProfile profile,
        PostflopBudget budget,
        CancellationToken cancellationToken)
    {
        List<ActionEvaluation> candidates =
        [
            new ActionEvaluation
            {
                Kind = PostflopActionKind.Fold,
                Amount = 0,
                ExpectedValue = 0,
                Equity = 0,
                FoldProbability = 0,
                Label = PostflopText.ActionFold,
                Explanation = PostflopText.FoldExplanation,
            },
            await EvaluateCallAsync(state, hero, pot, opponents, features, budget, cancellationToken).ConfigureAwait(false),
        ];

        foreach (double raiseTotal in RaiseSizes(pot))
        {
            candidates.Add(await EvaluateAggressionAsync(
                state,
                hero,
                pot,
                opponents,
                rankings,
                profile,
                budget,
                raiseTotal,
                PostflopActionKind.Raise,
                cancellationToken).ConfigureAwait(false));
        }

        return candidates;
    }

    private async Task<List<ActionEvaluation>> EvaluateUncontestedAsync(
        HandState state,
        HoleCards hero,
        PotSnapshot pot,
        IReadOnlyList<OpponentRange> opponents,
        IReadOnlyList<IReadOnlyList<RankedCombo>> rankings,
        HandAnalysis analysis,
        OpponentProfile profile,
        PostflopBudget budget,
        CancellationToken cancellationToken)
    {
        EquityMeasurement measured = await EquityAgainstAsync(
            state,
            hero,
            [.. opponents.Select(entry => entry.Range)],
            budget,
            cancellationToken).ConfigureAwait(false);

        double realisation = IsHeroInPosition(state, analysis, opponents)
            ? _options.RealisationInPosition
            : _options.RealisationOutOfPosition;

        List<ActionEvaluation> candidates =
        [
            new ActionEvaluation
            {
                Kind = PostflopActionKind.Check,
                Amount = 0,
                ExpectedValue = measured.Equity * pot.Pot * realisation,
                Equity = measured.Equity,
                EquityStandardError = measured.StandardError,
                FoldProbability = 0,
                Label = PostflopText.ActionCheck,
                Explanation = PostflopText.CheckExplanation(measured.Equity, pot.Pot, realisation),
            },
        ];

        foreach (double bet in BetSizes(pot))
        {
            candidates.Add(await EvaluateAggressionAsync(
                state,
                hero,
                pot,
                opponents,
                rankings,
                profile,
                budget,
                bet,
                PostflopActionKind.Bet,
                cancellationToken).ConfigureAwait(false));
        }

        return candidates;
    }

    private async Task<ActionEvaluation> EvaluateCallAsync(
        HandState state,
        HoleCards hero,
        PotSnapshot pot,
        IReadOnlyList<OpponentRange> opponents,
        HandFeatures features,
        PostflopBudget budget,
        CancellationToken cancellationToken)
    {
        double call = pot.AmountToCall;
        EquityMeasurement measured = await EquityAgainstAsync(
            state,
            hero,
            [.. opponents.Select(entry => entry.Range)],
            budget,
            cancellationToken).ConfigureAwait(false);

        double showdownValue = (measured.Equity * (pot.Pot + call)) - call;
        double implied = ImpliedOddsBonus(features, pot, state.Board.Count);

        string impliedText = implied > 0 ? PostflopText.CallImpliedOdds(implied) : ".";

        return new ActionEvaluation
        {
            Kind = PostflopActionKind.Call,
            Amount = call,
            ExpectedValue = showdownValue + implied,
            Equity = measured.Equity,
            EquityStandardError = measured.StandardError,
            FoldProbability = 0,
            Label = PostflopText.ActionCall(call),
            Explanation = PostflopText.CallExplanation(measured.Equity, pot.RequiredEquityToCall) + impliedText,
        };
    }

    private async Task<ActionEvaluation> EvaluateAggressionAsync(
        HandState state,
        HoleCards hero,
        PotSnapshot pot,
        IReadOnlyList<OpponentRange> opponents,
        IReadOnlyList<IReadOnlyList<RankedCombo>> rankings,
        OpponentProfile profile,
        PostflopBudget budget,
        double amount,
        PostflopActionKind kind,
        CancellationToken cancellationToken)
    {
        double call = pot.AmountToCall;
        double potBeforeResponse = pot.Pot - call;

        List<RangeSplit> splits = [];
        foreach (IReadOnlyList<RankedCombo> ranked in rankings)
        {
            splits.Add(OpponentResponseModel.SplitFacingBet(ranked, potBeforeResponse, amount - call, profile));
        }

        double foldProbability = 1;
        foreach (RangeSplit split in splits)
        {
            foldProbability *= split.FoldProbability;
        }

        double expectedValue = foldProbability * pot.Pot;
        EquityMeasurement whenCalled = EquityMeasurement.Certain(0);

        if (foldProbability < 1)
        {
            whenCalled = await EquityAgainstAsync(
                state,
                hero,
                [.. splits.Select(split => split.Continuing).Where(range => !range.IsEmpty)],
                budget,
                cancellationToken).ConfigureAwait(false);

            expectedValue += opponents.Count == 1
                ? HeadsUpContinuation(pot, splits[0], amount, call, whenCalled.Equity)
                : MultiwayContinuation(pot, splits, amount, call, whenCalled.Equity, foldProbability);
        }

        string sizing = PostflopText.Sizing(amount, pot.Pot <= 0 ? 0 : amount / pot.Pot);

        return new ActionEvaluation
        {
            Kind = kind,
            Amount = amount,
            ExpectedValue = expectedValue,
            Equity = whenCalled.Equity,
            EquityStandardError = whenCalled.StandardError,
            FoldProbability = foldProbability,
            Label = kind == PostflopActionKind.Bet
                ? PostflopText.ActionBet(sizing)
                : PostflopText.ActionRaise(sizing),
            Explanation = PostflopText.AggressionExplanation(foldProbability, whenCalled.Equity),
        };
    }

    private double HeadsUpContinuation(
        PotSnapshot pot,
        RangeSplit split,
        double amount,
        double call,
        double equityWhenCalled)
    {
        double calledPot = pot.Pot + amount + (amount - call);
        double callBranch = split.CallProbability * ((equityWhenCalled * calledPot) - amount);

        double reRaisePot = pot.Pot + (4 * amount);
        double reRaiseBranch = split.RaiseProbability
            * Math.Max(-amount, (equityWhenCalled * reRaisePot) - (2 * amount));

        return callBranch + reRaiseBranch;
    }

    private static double MultiwayContinuation(
        PotSnapshot pot,
        IReadOnlyList<RangeSplit> splits,
        double amount,
        double call,
        double equityWhenCalled,
        double foldProbability)
    {
        double expectedCallers = 0;
        foreach (RangeSplit split in splits)
        {
            expectedCallers += 1 - split.FoldProbability;
        }

        double contestedPot = pot.Pot + amount + (expectedCallers * (amount - call));

        return (1 - foldProbability) * ((equityWhenCalled * contestedPot) - amount);
    }

    private double ImpliedOddsBonus(HandFeatures features, PotSnapshot pot, int boardCardCount)
    {
        if (boardCardCount >= 5 || !features.HasDraw || features.IsStrongMadeHand)
        {
            return 0;
        }

        double remaining = Math.Max(0, pot.EffectiveStack - pot.AmountToCall);
        double payoff = Math.Min(remaining, pot.Pot + pot.AmountToCall);

        return features.ImprovementChance(boardCardCount) * _options.ImpliedOddsFactor * payoff;
    }

    private async Task<EquityMeasurement> EquityAgainstAsync(
        HandState state,
        HoleCards hero,
        IReadOnlyList<HandRange> villainRanges,
        PostflopBudget budget,
        CancellationToken cancellationToken)
    {
        if (villainRanges.Count == 0)
        {
            return EquityMeasurement.Certain(1);
        }

        HandRange heroRange = new HandRangeBuilder().Set(hero, 1).Build();

        EquityResult result = await _equity.ComputeAsync(
            new EquityRequest
            {
                PlayerRanges = [heroRange, .. villainRanges],
                Board = state.Board,
                Method = EquityMethod.MonteCarlo,
                MaximumSamples = budget.EquitySamples,
                RandomSeed = _options.RandomSeed,
                TargetStandardError = 0,
            },
            cancellationToken).ConfigureAwait(false);

        return new EquityMeasurement(result.Hero.Equity, result.HeroStandardError);
    }

    private IEnumerable<double> BetSizes(PotSnapshot pot)
    {
        HashSet<double> sizes = [];

        foreach (double fraction in _options.BetSizesAsPotFraction)
        {
            double size = Math.Round(Math.Min(pot.Pot * fraction, pot.RemainingStack), 2);
            if (size > 0)
            {
                sizes.Add(size);
            }
        }

        if (pot.RemainingStack > 0)
        {
            sizes.Add(Math.Round(pot.RemainingStack, 2));
        }

        return sizes;
    }

    private IEnumerable<double> RaiseSizes(PotSnapshot pot)
    {
        HashSet<double> sizes = [];

        double target = Math.Round(
            Math.Min(pot.AmountToCall + (pot.Pot * _options.RaiseSizeAsPotFraction), pot.RemainingStack),
            2);

        if (target > pot.AmountToCall)
        {
            sizes.Add(target);
        }

        if (pot.RemainingStack > pot.AmountToCall)
        {
            sizes.Add(Math.Round(pot.RemainingStack, 2));
        }

        return sizes;
    }

    private static bool IsHeroInPosition(
        HandState state,
        HandAnalysis analysis,
        IReadOnlyList<OpponentRange> opponents)
    {
        foreach (OpponentRange opponent in opponents)
        {
            if (!PositionLayout.ActsAfterPostflop(state.Table.PlayerCount, analysis.HeroPosition, opponent.Position))
            {
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<string> BuildRationale(
        PotSnapshot pot,
        HandFeatures features,
        BoardTexture texture,
        IReadOnlyList<OpponentRange> opponents,
        ActionEvaluation best,
        bool isClose,
        OpponentProfile profile,
        int boardCardCount)
    {
        List<string> lines =
        [
            PostflopText.RationaleHand(features.Describe()),
            texture.Describe() + ".",
            PostflopText.RationalePot(pot.Pot, pot.PotInBigBlinds, pot.EffectiveStack, pot.StackToPotRatio),
        ];

        if (pot.IsFacingABet)
        {
            lines.Add(PostflopText.RationaleFacingBet(
                pot.AmountToCall,
                pot.RequiredEquityToCall,
                pot.MinimumDefenceFrequency));
        }

        if (features.Outs > 0 && boardCardCount < 5)
        {
            lines.Add(PostflopText.RationaleOuts(features.Outs, features.ImprovementChance(boardCardCount)));
        }

        foreach (OpponentRange opponent in opponents)
        {
            lines.Add($"{PositionLayout.Describe(opponent.Position)} — {string.Join(" ", opponent.Story)}");
        }

        lines.Add(PostflopText.RationaleOpponentModel(profile.Name));
        lines.Add(best.Explanation);

        if (isClose)
        {
            lines.Add(PostflopText.RationaleCloseCall);
        }

        return lines;
    }
}
