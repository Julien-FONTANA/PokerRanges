using PokerRanges.Core.Cards;
using PokerRanges.Core.Ranges;

namespace PokerRanges.Core.Postflop;

public interface IRangeStrengthRanker
{
    /// <summary>
    /// Ranks a range's combos from strongest to weakest on this particular board.
    /// </summary>
    IReadOnlyList<RankedCombo> Rank(
        HandRange range,
        IReadOnlyList<Card> board,
        IReadOnlyList<Card> deadCards,
        PostflopBudget budget,
        CancellationToken cancellationToken);
}
