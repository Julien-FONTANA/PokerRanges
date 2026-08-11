using PokerRanges.Core.Cards;

namespace PokerRanges.Core.Evaluation;

public interface IMadeHandClassifier
{
    HandFeatures Classify(HoleCards hole, ReadOnlySpan<Card> board);
}
