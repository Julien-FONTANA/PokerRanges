using PokerRanges.Core.Cards;

namespace PokerRanges.Core.Evaluation;

/// <summary>
/// Evaluates 5 to 7 cards by counting ranks and suits, with no bitwise tricks and no precomputed
/// lookup table. No allocation: every counter lives on the stack.
/// </summary>
public sealed class RankCountHandEvaluator : IHandEvaluator
{
    private const int RankSlots = 15;
    private const int SuitCount = 4;
    private const int LowestRank = 2;
    private const int HighestRank = 14;

    public HandValue Evaluate(ReadOnlySpan<Card> cards)
    {
        if (cards.Length is < 5 or > 7)
        {
            throw new ArgumentException($"Evaluation expects 5 to 7 cards, got {cards.Length}.", nameof(cards));
        }

        Span<int> rankCounts = stackalloc int[RankSlots];
        Span<int> suitCounts = stackalloc int[SuitCount];
        Span<int> ranksBySuit = stackalloc int[SuitCount * RankSlots];
        rankCounts.Clear();
        suitCounts.Clear();
        ranksBySuit.Clear();

        foreach (Card card in cards)
        {
            rankCounts[(int)card.Rank]++;
            suitCounts[(int)card.Suit]++;
            ranksBySuit[((int)card.Suit * RankSlots) + (int)card.Rank]++;
        }

        int flushSuit = FindFlushSuit(suitCounts);

        if (flushSuit >= 0)
        {
            int straightFlushHigh = FindStraightHigh(ranksBySuit.Slice(flushSuit * RankSlots, RankSlots));
            if (straightFlushHigh > 0)
            {
                return HandValue.Create(HandCategory.StraightFlush, straightFlushHigh, 0, 0, 0, 0);
            }
        }

        int quadRank = HighestRankWithCount(rankCounts, 4, 0);
        if (quadRank > 0)
        {
            Span<int> kickers = stackalloc int[1];
            TakeTopRanks(rankCounts, quadRank, 0, kickers);
            return HandValue.Create(HandCategory.FourOfAKind, quadRank, kickers[0], 0, 0, 0);
        }

        int tripsRank = HighestRankWithCount(rankCounts, 3, 0);
        if (tripsRank > 0)
        {
            int pairedRank = HighestRankWithMinimumCount(rankCounts, 2, tripsRank);
            if (pairedRank > 0)
            {
                return HandValue.Create(HandCategory.FullHouse, tripsRank, pairedRank, 0, 0, 0);
            }
        }

        if (flushSuit >= 0)
        {
            Span<int> flushRanks = stackalloc int[5];
            TakeTopRanks(ranksBySuit.Slice(flushSuit * RankSlots, RankSlots), 0, 0, flushRanks);
            return HandValue.Create(
                HandCategory.Flush,
                flushRanks[0],
                flushRanks[1],
                flushRanks[2],
                flushRanks[3],
                flushRanks[4]);
        }

        int straightHigh = FindStraightHigh(rankCounts);
        if (straightHigh > 0)
        {
            return HandValue.Create(HandCategory.Straight, straightHigh, 0, 0, 0, 0);
        }

        if (tripsRank > 0)
        {
            Span<int> kickers = stackalloc int[2];
            TakeTopRanks(rankCounts, tripsRank, 0, kickers);
            return HandValue.Create(HandCategory.ThreeOfAKind, tripsRank, kickers[0], kickers[1], 0, 0);
        }

        int topPair = HighestRankWithCount(rankCounts, 2, 0);
        if (topPair > 0)
        {
            int secondPair = HighestRankWithCount(rankCounts, 2, topPair);
            if (secondPair > 0)
            {
                Span<int> kickers = stackalloc int[1];
                TakeTopRanks(rankCounts, topPair, secondPair, kickers);
                return HandValue.Create(HandCategory.TwoPair, topPair, secondPair, kickers[0], 0, 0);
            }

            Span<int> pairKickers = stackalloc int[3];
            TakeTopRanks(rankCounts, topPair, 0, pairKickers);
            return HandValue.Create(
                HandCategory.OnePair,
                topPair,
                pairKickers[0],
                pairKickers[1],
                pairKickers[2],
                0);
        }

        Span<int> highCards = stackalloc int[5];
        TakeTopRanks(rankCounts, 0, 0, highCards);
        return HandValue.Create(
            HandCategory.HighCard,
            highCards[0],
            highCards[1],
            highCards[2],
            highCards[3],
            highCards[4]);
    }

    private static int FindFlushSuit(ReadOnlySpan<int> suitCounts)
    {
        for (int suit = 0; suit < SuitCount; suit++)
        {
            if (suitCounts[suit] >= 5)
            {
                return suit;
            }
        }

        return -1;
    }

    /// <summary>
    /// Returns the top rank of a run of five consecutive ranks, 0 if there is none. The ace also
    /// counts as a low rank, which covers the wheel A-2-3-4-5 (returned as a five-high straight).
    /// </summary>
    private static int FindStraightHigh(ReadOnlySpan<int> rankCounts)
    {
        int consecutive = rankCounts[HighestRank] > 0 ? 1 : 0;
        int best = 0;

        for (int rank = LowestRank; rank <= HighestRank; rank++)
        {
            if (rankCounts[rank] > 0)
            {
                consecutive++;
                if (consecutive >= 5)
                {
                    best = rank;
                }
            }
            else
            {
                consecutive = 0;
            }
        }

        return best;
    }

    private static int HighestRankWithCount(ReadOnlySpan<int> rankCounts, int exactCount, int excludedRank)
    {
        for (int rank = HighestRank; rank >= LowestRank; rank--)
        {
            if (rank != excludedRank && rankCounts[rank] == exactCount)
            {
                return rank;
            }
        }

        return 0;
    }

    private static int HighestRankWithMinimumCount(ReadOnlySpan<int> rankCounts, int minimumCount, int excludedRank)
    {
        for (int rank = HighestRank; rank >= LowestRank; rank--)
        {
            if (rank != excludedRank && rankCounts[rank] >= minimumCount)
            {
                return rank;
            }
        }

        return 0;
    }

    private static void TakeTopRanks(
        ReadOnlySpan<int> rankCounts,
        int firstExcludedRank,
        int secondExcludedRank,
        Span<int> destination)
    {
        int written = 0;

        for (int rank = HighestRank; rank >= LowestRank && written < destination.Length; rank--)
        {
            if (rankCounts[rank] > 0 && rank != firstExcludedRank && rank != secondExcludedRank)
            {
                destination[written] = rank;
                written++;
            }
        }

        for (int index = written; index < destination.Length; index++)
        {
            destination[index] = 0;
        }
    }
}
