using PokerRanges.Core.Cards;
using PokerRanges.Core.Ranges;

namespace PokerRanges.Core.Postflop;

public interface IRangeStrengthRanker
{
    /// <summary>
    /// Classe les combos d'une range de la plus forte à la plus faible sur ce board précis.
    /// </summary>
    IReadOnlyList<RankedCombo> Rank(
        HandRange range,
        IReadOnlyList<Card> board,
        IReadOnlyList<Card> deadCards,
        PostflopBudget budget,
        CancellationToken cancellationToken);
}
