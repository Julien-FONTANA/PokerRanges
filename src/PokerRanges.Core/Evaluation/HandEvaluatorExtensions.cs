using PokerRanges.Core.Cards;

namespace PokerRanges.Core.Evaluation;

public static class HandEvaluatorExtensions
{
    public static HandValue EvaluateHand(this IHandEvaluator evaluator, HoleCards hole, ReadOnlySpan<Card> board)
    {
        ArgumentNullException.ThrowIfNull(evaluator);

        Span<Card> cards = stackalloc Card[board.Length + 2];
        cards[0] = hole.First;
        cards[1] = hole.Second;
        board.CopyTo(cards[2..]);

        return evaluator.Evaluate(cards);
    }
}
