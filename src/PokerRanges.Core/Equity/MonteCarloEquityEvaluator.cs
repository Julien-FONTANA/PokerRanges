using PokerRanges.Core.Cards;
using PokerRanges.Core.Evaluation;
using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Equity;

/// <summary>
/// Tire des mains au hasard dans les ranges par rejet : un échantillon dont les combos entrent en
/// conflit est rejeté en entier, ce qui reproduit exactement la loi jointe des ranges compatibles.
/// Le tirage progresse par vagues et s'arrête dès que l'erreur-type visée sur le héros est atteinte.
/// </summary>
internal sealed class MonteCarloEquityEvaluator
{
    private const int MaximumDealAttempts = 500;
    private const int CancellationCheckInterval = 8_192;
    private const long MinimumSamplesBeforeConvergenceCheck = 10_000;

    private readonly IHandEvaluator _evaluator;
    private readonly PlayerCombos[] _players;
    private readonly IReadOnlyList<Card> _knownBoard;
    private readonly bool[] _blockedCards;

    public MonteCarloEquityEvaluator(
        IHandEvaluator evaluator,
        PlayerCombos[] players,
        IReadOnlyList<Card> knownBoard,
        bool[] blockedCards)
    {
        _evaluator = evaluator;
        _players = players;
        _knownBoard = knownBoard;
        _blockedCards = blockedCards;
    }

    public EquityAccumulator Run(
        int maximumSamples,
        double targetStandardError,
        int? seed,
        CancellationToken cancellationToken)
    {
        int workerCount = seed.HasValue ? 1 : Math.Max(1, Environment.ProcessorCount - 1);
        int samplesPerWorkerPerRound = Math.Max(2_000, maximumSamples / (workerCount * 20));

        EquityAccumulator total = new(_players.Length);
        int drawn = 0;
        int round = 0;

        while (drawn < maximumSamples)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int remaining = maximumSamples - drawn;
            int perWorker = Math.Min(samplesPerWorkerPerRound, Math.Max(1, remaining / workerCount));
            EquityAccumulator[] partials = new EquityAccumulator[workerCount];

            RunRound(partials, perWorker, seed, round, cancellationToken);

            foreach (EquityAccumulator partial in partials)
            {
                total.Merge(partial);
            }

            drawn += perWorker * workerCount;
            round++;

            if (total.SampleCount >= MinimumSamplesBeforeConvergenceCheck
                && total.StandardErrorOf(0) <= targetStandardError)
            {
                break;
            }
        }

        return total;
    }

    private void RunRound(
        EquityAccumulator[] partials,
        int samplesPerWorker,
        int? seed,
        int round,
        CancellationToken cancellationToken)
    {
        ParallelOptions options = new()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = partials.Length,
        };

        try
        {
            Parallel.For(0, partials.Length, options, worker =>
            {
                int workerSeed = seed.HasValue
                    ? seed.Value + (round * 1_000_003) + worker
                    : Random.Shared.Next();

                partials[worker] = RunWorker(samplesPerWorker, workerSeed, cancellationToken);
            });
        }
        catch (AggregateException exception) when (exception.InnerException is EquityException inner)
        {
            throw inner;
        }
    }

    private EquityAccumulator RunWorker(int samples, int seed, CancellationToken cancellationToken)
    {
        Random random = new(seed);
        EquityAccumulator accumulator = new(_players.Length);

        bool[] used = new bool[Card.Count];
        HoleCards[] assigned = new HoleCards[_players.Length];
        Card[] board = new Card[5];
        HandValue[] values = new HandValue[_players.Length];
        Card[] hand = new Card[7];

        for (int sample = 0; sample < samples; sample++)
        {
            if (sample % CancellationCheckInterval == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (!TryDeal(random, used, assigned, board))
            {
                throw new EquityException(TableText.RangesOverlapTooMuch);
            }

            for (int player = 0; player < _players.Length; player++)
            {
                hand[0] = assigned[player].First;
                hand[1] = assigned[player].Second;
                board.CopyTo(hand, 2);
                values[player] = _evaluator.Evaluate(hand);
            }

            accumulator.AddShowdown(values, 1.0);
        }

        return accumulator;
    }

    private bool TryDeal(Random random, bool[] used, HoleCards[] assigned, Card[] board)
    {
        for (int attempt = 0; attempt < MaximumDealAttempts; attempt++)
        {
            Array.Copy(_blockedCards, used, used.Length);

            if (!TryAssignHoleCards(random, used, assigned))
            {
                continue;
            }

            for (int index = 0; index < _knownBoard.Count; index++)
            {
                board[index] = _knownBoard[index];
            }

            for (int index = _knownBoard.Count; index < board.Length; index++)
            {
                int cardIndex;
                do
                {
                    cardIndex = random.Next(Card.Count);
                }
                while (used[cardIndex]);

                used[cardIndex] = true;
                board[index] = Card.FromIndex(cardIndex);
            }

            return true;
        }

        return false;
    }

    private bool TryAssignHoleCards(Random random, bool[] used, HoleCards[] assigned)
    {
        for (int player = 0; player < _players.Length; player++)
        {
            HoleCards combo = _players[player].Sample(random.NextDouble());
            int first = combo.First.Index;
            int second = combo.Second.Index;

            if (used[first] || used[second])
            {
                return false;
            }

            used[first] = true;
            used[second] = true;
            assigned[player] = combo;
        }

        return true;
    }
}
