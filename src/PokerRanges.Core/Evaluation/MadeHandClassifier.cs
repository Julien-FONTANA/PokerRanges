using PokerRanges.Core.Cards;

namespace PokerRanges.Core.Evaluation;

/// <summary>
/// Qualifies the hero's hand against the board. Outs and whether it is the best possible hand are
/// counted by exhaustive enumeration rather than by a lookup table: that is exact on every board,
/// paired or monotone included, and it stays instant.
/// </summary>
public sealed class MadeHandClassifier : IMadeHandClassifier
{
    private readonly IHandEvaluator _evaluator;

    public MadeHandClassifier(IHandEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    public HandFeatures Classify(HoleCards hole, ReadOnlySpan<Card> board)
    {
        if (board.Length is < 3 or > 5)
        {
            throw new ArgumentException($"La classification attend un board de 3 à 5 cartes, reçu {board.Length}.", nameof(board));
        }

        Span<Card> current = stackalloc Card[board.Length + 2];
        current[0] = hole.First;
        current[1] = hole.Second;
        board.CopyTo(current[2..]);

        HandValue value = _evaluator.Evaluate(current);
        MadeHandTier tier = DetermineTier(hole, board, value, out Rank? pairedRank);

        Span<bool> used = stackalloc bool[Card.Count];
        used.Clear();
        used[hole.First.Index] = true;
        used[hole.Second.Index] = true;
        foreach (Card card in board)
        {
            used[card.Index] = true;
        }

        int outs = CountOuts(hole, board, used, value, tier, out int straightOuts);

        return new HandFeatures
        {
            Value = value,
            Tier = tier,
            IsNuts = IsBestPossible(board, used, value),
            Outs = outs,
            StraightOuts = straightOuts,
            HasFlushDraw = HasFlushDraw(hole, board),
            HasOpenEndedStraightDraw = straightOuts >= 8,
            HasGutshot = straightOuts is > 0 and < 8,
            PairedRank = pairedRank,
        };
    }

    private static MadeHandTier DetermineTier(
        HoleCards hole,
        ReadOnlySpan<Card> board,
        HandValue value,
        out Rank? pairedRank)
    {
        pairedRank = null;

        switch (value.Category)
        {
            case HandCategory.StraightFlush:
                return MadeHandTier.StraightFlush;
            case HandCategory.FourOfAKind:
                return MadeHandTier.Quads;
            case HandCategory.FullHouse:
                return MadeHandTier.FullHouse;
            case HandCategory.Flush:
                return MadeHandTier.Flush;
            case HandCategory.Straight:
                return MadeHandTier.Straight;
            case HandCategory.ThreeOfAKind:
                int tripsRank = HandValue.TiebreakAt(value.Strength, 1);
                pairedRank = (Rank)tripsRank;
                return hole.First.Rank == hole.Second.Rank && (int)hole.First.Rank == tripsRank
                    ? MadeHandTier.Set
                    : MadeHandTier.Trips;
            case HandCategory.TwoPair:
                return MadeHandTier.TwoPair;
            case HandCategory.OnePair:
                Rank pairRank = (Rank)HandValue.TiebreakAt(value.Strength, 1);
                pairedRank = pairRank;
                return ClassifyPair(hole, board, pairRank);
            default:
                return MadeHandTier.HighCard;
        }
    }

    private static MadeHandTier ClassifyPair(HoleCards hole, ReadOnlySpan<Card> board, Rank pairRank)
    {
        if (hole.First.Rank == hole.Second.Rank && hole.First.Rank == pairRank)
        {
            Rank highestBoardRank = Rank.Two;
            foreach (Card card in board)
            {
                highestBoardRank = card.Rank > highestBoardRank ? card.Rank : highestBoardRank;
            }

            return pairRank > highestBoardRank ? MadeHandTier.Overpair : MadeHandTier.UnderPair;
        }

        int position = 0;

        for (Rank rank = Rank.Ace; rank >= Rank.Two; rank--)
        {
            bool present = false;
            foreach (Card card in board)
            {
                if (card.Rank == rank)
                {
                    present = true;
                    break;
                }
            }

            if (!present)
            {
                continue;
            }

            if (rank == pairRank)
            {
                return position == 0 ? MadeHandTier.TopPair
                    : position == 1 ? MadeHandTier.MiddlePair
                    : MadeHandTier.BottomPair;
            }

            position++;
        }

        return MadeHandTier.UnderPair;
    }

    /// <summary>
    /// An out is a card that, on the next street, takes the hand to two pair or better and improves
    /// on its current strength: the working definition that gives 9 outs to a flush draw, 8 to an
    /// open-ender, 4 to a gutshot and 15 to a combo draw, with no double counting.
    /// </summary>
    private int CountOuts(
        HoleCards hole,
        ReadOnlySpan<Card> board,
        ReadOnlySpan<bool> used,
        HandValue current,
        MadeHandTier currentTier,
        out int straightOuts)
    {
        straightOuts = 0;

        if (board.Length >= 5)
        {
            return 0;
        }

        Span<Card> hand = stackalloc Card[board.Length + 3];
        hand[0] = hole.First;
        hand[1] = hole.Second;
        board.CopyTo(hand[2..]);

        bool alreadyStraight = ContainsStraight(hand[..^1]);
        int outs = 0;

        for (int index = 0; index < Card.Count; index++)
        {
            if (used[index])
            {
                continue;
            }

            hand[^1] = Card.FromIndex(index);
            HandValue improved = _evaluator.Evaluate(hand);

            if (improved <= current || improved.Category < HandCategory.TwoPair)
            {
                continue;
            }

            outs++;

            if (!alreadyStraight && ContainsStraight(hand))
            {
                straightOuts++;
            }
        }

        return outs;
    }

    /// <summary>
    /// Looks for five consecutive ranks without regard to suit: a card completing a straight flush
    /// also completes a straight, and must be counted as a straight out.
    /// </summary>
    private static bool ContainsStraight(ReadOnlySpan<Card> cards)
    {
        Span<bool> present = stackalloc bool[15];
        present.Clear();

        foreach (Card card in cards)
        {
            present[(int)card.Rank] = true;
        }

        int consecutive = present[(int)Rank.Ace] ? 1 : 0;

        for (int rank = (int)Rank.Two; rank <= (int)Rank.Ace; rank++)
        {
            if (!present[rank])
            {
                consecutive = 0;
                continue;
            }

            consecutive++;
            if (consecutive >= 5)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasFlushDraw(HoleCards hole, ReadOnlySpan<Card> board)
    {
        if (board.Length >= 5)
        {
            return false;
        }

        Span<int> suitCounts = stackalloc int[4];
        Span<int> heroSuits = stackalloc int[4];
        suitCounts.Clear();
        heroSuits.Clear();

        suitCounts[(int)hole.First.Suit]++;
        suitCounts[(int)hole.Second.Suit]++;
        heroSuits[(int)hole.First.Suit]++;
        heroSuits[(int)hole.Second.Suit]++;

        foreach (Card card in board)
        {
            suitCounts[(int)card.Suit]++;
        }

        for (int suit = 0; suit < 4; suit++)
        {
            if (suitCounts[suit] == 4 && heroSuits[suit] > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True if no opponent can hold better. The hero's cards are excluded from the possible hands:
    /// what matters when deciding is what the opponent can actually have, blockers included.
    /// </summary>
    private bool IsBestPossible(ReadOnlySpan<Card> board, ReadOnlySpan<bool> used, HandValue heroValue)
    {
        Span<Card> hand = stackalloc Card[board.Length + 2];
        board.CopyTo(hand[2..]);

        for (int first = 0; first < Card.Count; first++)
        {
            if (used[first])
            {
                continue;
            }

            hand[0] = Card.FromIndex(first);

            for (int second = first + 1; second < Card.Count; second++)
            {
                if (used[second])
                {
                    continue;
                }

                hand[1] = Card.FromIndex(second);

                if (_evaluator.Evaluate(hand) > heroValue)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
