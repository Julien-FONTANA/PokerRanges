using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Evaluation;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Ranges;

namespace PokerRanges.Core.Equity;

public sealed class EquityCalculator : IEquityCalculator
{
    /// <summary>
    /// Beyond this many showdowns, exhaustive enumeration costs more than a second: we switch to
    /// random sampling, whose precision is more than enough anyway.
    /// </summary>
    private const double ExhaustiveShowdownBudget = 5_000_000;

    private readonly IHandEvaluator _evaluator;
    private readonly ILogger<EquityCalculator> _logger;

    public EquityCalculator(IHandEvaluator evaluator, ILogger<EquityCalculator> logger)
    {
        _evaluator = evaluator;
        _logger = logger;
    }

    public async Task<EquityResult> ComputeAsync(EquityRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        return await Task.Run(() => Compute(request, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    private EquityResult Compute(EquityRequest request, CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        bool[] blockedCards = BuildBlockedCards(request);
        PlayerCombos[] players = BuildPlayers(request, blockedCards);
        bool useExhaustive = ShouldEnumerate(request, players);

        EquityAccumulator accumulator = useExhaustive
            ? new ExhaustiveEquityEvaluator(_evaluator, players, request.Board, blockedCards, cancellationToken).Run()
            : new MonteCarloEquityEvaluator(_evaluator, players, request.Board, blockedCards)
                .Run(request.MaximumSamples, request.TargetStandardError, request.RandomSeed, cancellationToken);

        if (accumulator.SampleCount == 0)
        {
            throw new EquityException(
                "No deal is possible: the ranges, the board and the dead cards are incompatible with each other.");
        }

        double standardError = useExhaustive ? 0 : accumulator.StandardErrorOf(0);
        stopwatch.Stop();

        _logger.LogDebug(
            "Equity computed by {Method}: {Samples} showdowns, hero equity {HeroEquity:P2} ± {Margin:P2}, in {ElapsedMilliseconds} ms",
            useExhaustive ? "exhaustive enumeration" : "Monte-Carlo",
            accumulator.SampleCount,
            accumulator.EquityOf(0),
            1.96 * standardError,
            stopwatch.ElapsedMilliseconds);

        return new EquityResult(
            accumulator.ToPlayerEquities(),
            accumulator.SampleCount,
            useExhaustive,
            standardError,
            stopwatch.Elapsed);
    }

    private static void Validate(EquityRequest request)
    {
        if (request.PlayerRanges.Count < 2)
        {
            throw new EquityException(TableText.EquityNeedsTwoPlayers(request.PlayerRanges.Count));
        }

        if (request.Board.Count is not (0 or 3 or 4 or 5))
        {
            throw new EquityException(TableText.BoardCardCount(request.Board.Count));
        }

        HashSet<Card> seen = [];
        foreach (Card card in request.Board.Concat(request.DeadCards))
        {
            if (!seen.Add(card))
            {
                throw new EquityException(TableText.CardTwiceInBoard(card));
            }
        }
    }

    private static bool[] BuildBlockedCards(EquityRequest request)
    {
        bool[] blocked = new bool[Card.Count];

        foreach (Card card in request.Board)
        {
            blocked[card.Index] = true;
        }

        foreach (Card card in request.DeadCards)
        {
            blocked[card.Index] = true;
        }

        return blocked;
    }

    private static PlayerCombos[] BuildPlayers(EquityRequest request, bool[] blockedCards)
    {
        PlayerCombos[] players = new PlayerCombos[request.PlayerRanges.Count];

        for (int index = 0; index < players.Length; index++)
        {
            HandRange range = request.PlayerRanges[index];
            players[index] = PlayerCombos.Create(range, blockedCards);

            if (players[index].Length == 0)
            {
                throw new EquityException(
                    $"Player {index + 1}'s range holds no combo at all once the board and dead cards are removed.");
            }
        }

        return players;
    }

    private bool ShouldEnumerate(EquityRequest request, PlayerCombos[] players)
    {
        if (request.Method == EquityMethod.MonteCarlo)
        {
            return false;
        }

        double assignments = 1;
        foreach (PlayerCombos player in players)
        {
            assignments *= player.Length;
        }

        int remainingCards = Card.Count - request.Board.Count - request.DeadCards.Count - (2 * players.Length);
        double showdowns = assignments * CountCombinations(remainingCards, 5 - request.Board.Count);

        if (request.Method == EquityMethod.Exhaustive)
        {
            if (showdowns > ExhaustiveShowdownBudget)
            {
                _logger.LogWarning(
                    "Exhaustive enumeration explicitly requested over roughly {Showdowns:N0} showdowns: this will be slow.",
                    showdowns);
            }

            return true;
        }

        return showdowns <= ExhaustiveShowdownBudget;
    }

    private static double CountCombinations(int total, int chosen)
    {
        if (chosen <= 0)
        {
            return 1;
        }

        if (chosen > total)
        {
            return 0;
        }

        double result = 1;
        for (int step = 1; step <= chosen; step++)
        {
            result = result * (total - chosen + step) / step;
        }

        return result;
    }
}
