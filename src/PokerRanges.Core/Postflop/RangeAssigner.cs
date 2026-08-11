using Microsoft.Extensions.Logging;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Preflop;
using PokerRanges.Core.Ranges;
using PokerRanges.Core.Table;

namespace PokerRanges.Core.Postflop;

/// <summary>
/// Rebuilds the range of every opponent still in the hand: start from the preflop chart matching
/// the action they actually took, strip the combos the board makes impossible, then narrow at each
/// postflop action according to the response model.
/// </summary>
public sealed class RangeAssigner : IRangeAssigner
{
    private readonly IPreflopChartRepository _charts;
    private readonly IPotEngine _potEngine;
    private readonly IRangeStrengthRanker _ranker;
    private readonly PreflopAdvisorOptions _preflopOptions;
    private readonly ILogger<RangeAssigner> _logger;

    public RangeAssigner(
        IPreflopChartRepository charts,
        IPotEngine potEngine,
        IRangeStrengthRanker ranker,
        PreflopAdvisorOptions preflopOptions,
        ILogger<RangeAssigner> logger)
    {
        _charts = charts;
        _potEngine = potEngine;
        _ranker = ranker;
        _preflopOptions = preflopOptions;
        _logger = logger;
    }

    public IReadOnlyList<OpponentRange> Assign(
        HandState state,
        OpponentProfile profile,
        PostflopBudget budget,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(budget);

        HandAnalysis analysis = _potEngine.Analyse(state);
        Card[] deadCards = BuildDeadCards(state);
        List<OpponentRange> assigned = [];

        foreach (Position opponent in analysis.LiveOpponents)
        {
            List<string> story = [];
            HandRange range = ReadPreflopRange(state, opponent, story).WithoutCards(deadCards);

            foreach (PlayerAction action in state.Actions)
            {
                if (action.Street == Street.Preflop || action.Position != opponent)
                {
                    continue;
                }

                range = Narrow(state, action, range, profile, budget, story, cancellationToken);
            }

            story.Add(PostflopText.StoryFinalRange(range.TotalCombos, range.PercentOfAllHands));
            assigned.Add(new OpponentRange(opponent, range, story));
        }

        _logger.LogDebug("Ranges assignées à {Count} adversaire(s).", assigned.Count);

        return assigned;
    }

    private static Card[] BuildDeadCards(HandState state)
    {
        List<Card> dead = [.. state.Board];

        if (state.HeroCards is HoleCards hero)
        {
            dead.Add(hero.First);
            dead.Add(hero.Second);
        }

        return [.. dead];
    }

    private HandRange ReadPreflopRange(HandState state, Position opponent, List<string> story)
    {
        PlayerAction? lastPreflop = null;
        int actionIndex = -1;

        for (int index = 0; index < state.Actions.Count; index++)
        {
            PlayerAction action = state.Actions[index];
            if (action.Street == Street.Preflop && action.Position == opponent)
            {
                lastPreflop = action;
                actionIndex = index;
            }
        }

        if (lastPreflop is null)
        {
            story.Add(PostflopText.StoryNoActionYet(PositionLayout.Describe(opponent)));
            return HandRange.Full;
        }

        HandState upToAction = state with
        {
            Board = [],
            Actions = [.. state.Actions.Take(actionIndex)],
            Table = state.Table with { HeroPosition = opponent },
        };

        HandAnalysis analysis = _potEngine.Analyse(upToAction);
        PreflopSituation situation = PreflopSituationReader.Read(
            upToAction,
            analysis,
            _preflopOptions.JamThresholdInBigBlinds);

        ChartResolution resolution = _charts.Resolve(situation.ToKey());
        HandRange range = MatchActionToRange(resolution, lastPreflop, out string how);

        story.Add(PostflopText.StoryOpening(PositionLayout.Describe(opponent), how, resolution.Describe()));

        return range;
    }

    private static HandRange MatchActionToRange(ChartResolution resolution, PlayerAction action, out string how)
    {
        HandRange raising = resolution.Strategy.RangeOf(ChartActionKind.Raise);
        HandRange jamming = resolution.Strategy.RangeOf(ChartActionKind.Jam);
        HandRange calling = resolution.Strategy.RangeOf(ChartActionKind.Call);
        HandRange everything = raising.Union(jamming).Union(calling);

        switch (action.Kind)
        {
            case PlayerActionKind.Bet:
            case PlayerActionKind.Raise:
                HandRange aggressive = raising.Union(jamming);
                if (!aggressive.IsEmpty)
                {
                    how = PostflopText.StoryRaisedPreflop;
                    return aggressive;
                }

                break;

            case PlayerActionKind.Call:
                if (!calling.IsEmpty)
                {
                    how = PostflopText.StoryCalledPreflop;
                    return calling;
                }

                break;

            case PlayerActionKind.Check:
                how = PostflopText.StoryCheckedOption;
                return HandRange.Full.Except(raising.Union(jamming));

            default:
                break;
        }

        how = PostflopText.StoryActionMissingFromChart(DescribeAction(action.Kind));
        return everything.IsEmpty ? HandRange.Full : everything;
    }

    private HandRange Narrow(
        HandState state,
        PlayerAction action,
        HandRange range,
        OpponentProfile profile,
        PostflopBudget budget,
        List<string> story,
        CancellationToken cancellationToken)
    {
        if (action.Kind is PlayerActionKind.Check)
        {
            story.Add(PostflopText.StoryCheckKeepsRange(TableText.Describe(action.Street)));
            return range;
        }

        Card[] board = [.. state.Board.Take(BoardCardsOn(action.Street))];
        Card[] deadCards = state.HeroCards is HoleCards hero ? [hero.First, hero.Second] : [];

        IReadOnlyList<RankedCombo> ranked = _ranker.Rank(range, board, deadCards, budget, cancellationToken);

        if (ranked.Count == 0)
        {
            return range;
        }

        if (action.Kind is PlayerActionKind.Bet or PlayerActionKind.Raise)
        {
            HandRange betting = OpponentResponseModel.BettingRange(ranked, profile);
            story.Add(PostflopText.StoryPolarised(
                TableText.Describe(action.Street),
                action.AmountTo,
                betting.TotalCombos));
            return betting;
        }

        HandState beforeCall = TruncateBefore(state, action);
        HandAnalysis analysis = _potEngine.Analyse(beforeCall);
        PotSnapshot snapshot = analysis.For(action.Position);

        RangeSplit split = OpponentResponseModel.SplitFacingBet(
            ranked,
            snapshot.Pot - snapshot.AmountToCall,
            snapshot.AmountToCall,
            profile);

        story.Add(PostflopText.StoryCalls(
            TableText.Describe(action.Street),
            snapshot.AmountToCall,
            split.CallProbability));

        return split.Calling;
    }

    private static HandState TruncateBefore(HandState state, PlayerAction action)
    {
        int index = 0;
        for (int position = 0; position < state.Actions.Count; position++)
        {
            if (ReferenceEquals(state.Actions[position], action))
            {
                index = position;
                break;
            }
        }

        return state with
        {
            Board = [.. state.Board.Take(BoardCardsOn(action.Street))],
            Actions = [.. state.Actions.Take(index)],
        };
    }

    private static int BoardCardsOn(Street street)
    {
        return street switch
        {
            Street.Preflop => 0,
            Street.Flop => 3,
            Street.Turn => 4,
            _ => 5,
        };
    }

    private static string DescribeAction(PlayerActionKind kind)
    {
        return kind switch
        {
            PlayerActionKind.Fold => PostflopText.ActionFolded,
            PlayerActionKind.Check => PostflopText.ActionChecked,
            PlayerActionKind.Call => PostflopText.ActionCalled,
            PlayerActionKind.Bet => PostflopText.ActionBetted,
            _ => PostflopText.ActionRaised,
        };
    }
}
