using Microsoft.Extensions.Logging;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Equity;
using PokerRanges.Core.Evaluation;
using PokerRanges.Core.Ranges;

namespace PokerRanges.Core.Postflop;

/// <summary>
/// Ranks a range by measuring, for each combo, its equity against the range itself on this board.
/// Ranking by equity values draws naturally, where a hand-written bonus table systematically
/// underrates a flush draw against a small pair.
/// Sampling uses a fixed seed: the same situation yields the same ranking.
/// </summary>
public sealed class RangeStrengthRanker : IRangeStrengthRanker
{
    private const int MaximumDealAttempts = 60;

    private readonly IHandEvaluator _evaluator;
    private readonly PostflopOptions _options;
    private readonly ILogger<RangeStrengthRanker> _logger;

    public RangeStrengthRanker(
        IHandEvaluator evaluator,
        PostflopOptions options,
        ILogger<RangeStrengthRanker> logger)
    {
        _evaluator = evaluator;
        _options = options;
        _logger = logger;
    }

    public IReadOnlyList<RankedCombo> Rank(
        HandRange range,
        IReadOnlyList<Card> board,
        IReadOnlyList<Card> deadCards,
        PostflopBudget budget,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(range);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(deadCards);
        ArgumentNullException.ThrowIfNull(budget);

        bool[] blocked = new bool[Card.Count];
        foreach (Card card in board.Concat(deadCards))
        {
            blocked[card.Index] = true;
        }

        PlayerCombos combos = PlayerCombos.Create(range, blocked);

        if (combos.Length == 0)
        {
            return [];
        }

        RankedCombo[] ranked = new RankedCombo[combos.Length];

        Parallel.For(
            0,
            combos.Length,
            new ParallelOptions { CancellationToken = cancellationToken },
            index => ranked[index] = Measure(combos, index, board, blocked, budget));

        Array.Sort(ranked, static (left, right) => right.Equity.CompareTo(left.Equity));

        _logger.LogDebug(
            "Range of {Count} combos ranked on {Board}: from {Best:P1} to {Worst:P1} equity.",
            ranked.Length,
            string.Join(string.Empty, board),
            ranked[0].Equity,
            ranked[^1].Equity);

        return ranked;
    }

    private RankedCombo Measure(
        PlayerCombos combos,
        int index,
        IReadOnlyList<Card> board,
        bool[] blocked,
        PostflopBudget budget)
    {
        HoleCards hero = combos.Combos[index];
        Random random = new(_options.RandomSeed + hero.Index);

        Span<bool> used = stackalloc bool[Card.Count];
        Span<Card> heroHand = stackalloc Card[7];
        Span<Card> villainHand = stackalloc Card[7];

        double shareSum = 0;
        int played = 0;

        for (int sample = 0; sample < budget.RankingSamplesPerCombo; sample++)
        {
            blocked.CopyTo(used);
            used[hero.First.Index] = true;
            used[hero.Second.Index] = true;

            if (!TryPickOpponent(combos, random, used, out HoleCards villain))
            {
                continue;
            }

            heroHand[0] = hero.First;
            heroHand[1] = hero.Second;
            villainHand[0] = villain.First;
            villainHand[1] = villain.Second;

            for (int position = 0; position < board.Count; position++)
            {
                heroHand[2 + position] = board[position];
                villainHand[2 + position] = board[position];
            }

            for (int position = board.Count; position < 5; position++)
            {
                int cardIndex;
                do
                {
                    cardIndex = random.Next(Card.Count);
                }
                while (used[cardIndex]);

                used[cardIndex] = true;
                Card drawn = Card.FromIndex(cardIndex);
                heroHand[2 + position] = drawn;
                villainHand[2 + position] = drawn;
            }

            HandValue heroValue = _evaluator.Evaluate(heroHand);
            HandValue villainValue = _evaluator.Evaluate(villainHand);

            shareSum += heroValue > villainValue ? 1 : heroValue == villainValue ? 0.5 : 0;
            played++;
        }

        double equity = played == 0 ? 0.5 : shareSum / played;

        return new RankedCombo(hero, combos.Weights[index], equity);
    }

    private static bool TryPickOpponent(PlayerCombos combos, Random random, Span<bool> used, out HoleCards villain)
    {
        for (int attempt = 0; attempt < MaximumDealAttempts; attempt++)
        {
            HoleCards candidate = combos.Sample(random.NextDouble());

            if (!used[candidate.First.Index] && !used[candidate.Second.Index])
            {
                used[candidate.First.Index] = true;
                used[candidate.Second.Index] = true;
                villain = candidate;
                return true;
            }
        }

        villain = default;
        return false;
    }
}
