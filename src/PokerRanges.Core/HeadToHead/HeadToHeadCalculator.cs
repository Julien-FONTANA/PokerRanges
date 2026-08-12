using Microsoft.Extensions.Logging;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Equity;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Preflop;
using PokerRanges.Core.Ranges;

namespace PokerRanges.Core.HeadToHead;

/// <summary>
/// Two ranges, one all-in, and what each option is worth in chips. Deliberately not ICM: at a final
/// table chips are the wrong currency, and the result says so rather than pretending otherwise.
/// </summary>
public sealed class HeadToHeadCalculator : IHeadToHeadCalculator
{
    private readonly IEquityCalculator _equity;
    private readonly ILogger<HeadToHeadCalculator> _logger;

    public HeadToHeadCalculator(IEquityCalculator equity, ILogger<HeadToHeadCalculator> logger)
    {
        _equity = equity;
        _logger = logger;
    }

    public async Task<HeadToHeadResult> ComputeAsync(
        HeadToHeadRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        HeadToHeadSpot spot = request.Spot;
        Card[] board = [.. request.Board];
        RejectEmptyRanges(request, board);

        List<string> rationale = [];
        double continueFrequency = ReadContinueFrequency(request, board, rationale);

        EquityResult equity = await _equity.ComputeAsync(
            new EquityRequest
            {
                PlayerRanges = [request.HeroRange, request.VillainRange],
                Board = request.Board,
                Method = request.Method,
                MaximumSamples = request.MaximumSamples,
                TargetStandardError = request.TargetStandardError,
                RandomSeed = request.RandomSeed,
            },
            cancellationToken).ConfigureAwait(false);

        double heroEquity = equity.Hero.Equity;
        double? breakEvenFold = ReadBreakEvenFoldFrequency(spot, heroEquity, rationale);

        rationale.Add(HeadToHeadText.SpotSummary(spot.EffectiveStack, spot.DepthInBigBlinds, spot.ContestedPot));
        if (spot.DeadChips > 0)
        {
            rationale.Add(HeadToHeadText.DeadMoney(spot.DeadChips));
        }

        if (spot.HeroIsAllIn)
        {
            rationale.Add(HeadToHeadText.HeroCannotFold);
        }
        else
        {
            rationale.Add(HeadToHeadText.BreakEvenEquity(spot.BreakEvenEquityIfCalled, spot.HeroRisk));
        }

        rationale.Add(HeadToHeadText.ChipsNotIcm);

        IReadOnlyList<HeadToHeadActionEvaluation> actions = BuildActions(spot, heroEquity, continueFrequency);

        _logger.LogDebug(
            "Head-to-head computed: hero equity {HeroEquity:P2} ± {Margin:P2}, villain continues {Continue:P1}, best {Best} worth {ExpectedValue} chips",
            heroEquity,
            1.96 * equity.HeroStandardError,
            continueFrequency,
            actions[0].Kind,
            actions[0].ExpectedValue);

        return new HeadToHeadResult
        {
            Spot = spot,
            Hero = equity.Hero,
            Villain = equity.Players[1],
            StandardError = equity.HeroStandardError,
            WasExhaustive = equity.WasExhaustive,
            SamplesEvaluated = equity.SamplesEvaluated,
            Duration = equity.Duration,
            VillainContinueFrequency = continueFrequency,
            BreakEvenFoldFrequency = breakEvenFold,
            Actions = actions,
            Rationale = rationale,
        };
    }

    private static void RejectEmptyRanges(HeadToHeadRequest request, ReadOnlySpan<Card> board)
    {
        // The equity engine would reject these too, but its message names a player number rather
        // than the side of the table the user is looking at.
        if (request.HeroRange.WithoutCards(board).IsEmpty)
        {
            throw new HeadToHeadException(HeadToHeadText.EmptyHeroRange);
        }

        if (request.VillainRange.WithoutCards(board).IsEmpty)
        {
            throw new HeadToHeadException(HeadToHeadText.EmptyVillainRange);
        }
    }

    /// <summary>
    /// How often the villain puts chips in rather than folding. His range <em>is</em> his continuing
    /// range, so the answer is its size measured against every hand he could hold.
    /// </summary>
    private static double ReadContinueFrequency(
        HeadToHeadRequest request,
        Card[] board,
        List<string> rationale)
    {
        if (request.Spot.Role == HeadToHeadRole.CallingAJam)
        {
            rationale.Add(HeadToHeadText.CallingRangeIsTheVillainsJam);
            return 1;
        }

        if (request.Spot.VillainIsAllIn)
        {
            rationale.Add(HeadToHeadText.VillainCannotFold);
            return 1;
        }

        if (request.VillainCards is not null)
        {
            rationale.Add(HeadToHeadText.VillainPinnedToOneHand);
            return 1;
        }

        // His own cards are not subtracted from the denominator: the hero's are, but only when the
        // hero holds a known hand. With a range on both sides there is no single pair of cards to
        // remove, and pretending otherwise would double-count the blocking.
        Card[] blockers = request.HeroCards is HoleCards hero
            ? [.. board, hero.First, hero.Second]
            : board;

        if (request.HeroCards is null)
        {
            rationale.Add(HeadToHeadText.HeroCardRemovalIgnored);
        }

        double possible = HandRange.Full.WithoutCards(blockers).TotalCombos;
        double continuing = request.VillainRange.WithoutCards(blockers).TotalCombos;
        double frequency = possible <= 0 ? 1 : Math.Clamp(continuing / possible, 0, 1);

        rationale.Add(HeadToHeadText.JammingRangeIsTheVillainsCall);
        rationale.Add(HeadToHeadText.VillainContinues(frequency, continuing));

        return frequency;
    }

    /// <summary>
    /// The fold frequency that makes a jam worth exactly zero. Solving
    /// <c>f·U + (1−f)·S = 0</c> for <c>f</c> gives <c>−S / (U − S)</c>, where <c>S</c> is the
    /// surplus once called. The early return matters: past the point where <c>S</c> turns positive
    /// the quotient leaves [0,1] entirely, and clamping it would report a jam that cannot lose as
    /// one needing every fold in the world.
    /// </summary>
    private static double? ReadBreakEvenFoldFrequency(
        HeadToHeadSpot spot,
        double heroEquity,
        List<string> rationale)
    {
        if (spot.Role != HeadToHeadRole.Jamming || spot.HeroIsAllIn)
        {
            return null;
        }

        double surplus = (heroEquity * spot.ContestedPot) - spot.HeroRisk;
        if (surplus >= 0)
        {
            rationale.Add(HeadToHeadText.JamProfitableWithoutAnyFold);
            return null;
        }

        // With surplus negative, U − S is strictly greater than −S, so the quotient is already
        // inside (0,1) and needs no clamping.
        double frequency = -surplus / (spot.UncontestedPot - surplus);
        rationale.Add(HeadToHeadText.BreakEvenFoldFrequency(frequency));

        return frequency;
    }

    private static IReadOnlyList<HeadToHeadActionEvaluation> BuildActions(
        HeadToHeadSpot spot,
        double heroEquity,
        double continueFrequency)
    {
        double surplus = (heroEquity * spot.ContestedPot) - spot.HeroRisk;

        if (spot.HeroIsAllIn)
        {
            // No fold to be worth zero: the chips are in and the hand plays itself out.
            return
            [
                new HeadToHeadActionEvaluation
                {
                    Kind = ChartActionKind.Jam,
                    Amount = 0,
                    ExpectedValue = heroEquity * spot.ContestedPot,
                    Label = HeadToHeadText.Showdown,
                    Explanation = HeadToHeadText.ShowdownExplanation(heroEquity),
                },
            ];
        }

        HeadToHeadActionEvaluation fold = new()
        {
            Kind = ChartActionKind.Fold,
            Amount = 0,
            ExpectedValue = 0,
            Label = PostflopText.ActionFold,
            Explanation = PostflopText.FoldExplanation,
        };

        double foldFrequency = 1 - continueFrequency;

        HeadToHeadActionEvaluation aggressive = spot.Role == HeadToHeadRole.Jamming
            ? new HeadToHeadActionEvaluation
            {
                Kind = ChartActionKind.Jam,
                Amount = spot.HeroRisk,
                ExpectedValue = (foldFrequency * spot.UncontestedPot) + (continueFrequency * surplus),
                Label = HeadToHeadText.ActionJam(spot.HeroRisk),
                Explanation = HeadToHeadText.JamExplanation(foldFrequency, heroEquity),
            }
            : new HeadToHeadActionEvaluation
            {
                Kind = ChartActionKind.Call,
                Amount = spot.HeroRisk,
                ExpectedValue = surplus,
                Label = PostflopText.ActionCall(spot.HeroRisk),
                Explanation = HeadToHeadText.CallExplanation(heroEquity, spot.BreakEvenEquityIfCalled),
            };

        // Ties keep insertion order, so acting stays ahead of folding when they are worth the same.
        return [.. new[] { aggressive, fold }.OrderByDescending(action => action.ExpectedValue)];
    }
}
