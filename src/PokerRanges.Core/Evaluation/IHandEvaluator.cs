using PokerRanges.Core.Cards;

namespace PokerRanges.Core.Evaluation;

public interface IHandEvaluator
{
    HandValue Evaluate(ReadOnlySpan<Card> cards);
}
