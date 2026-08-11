using PokerRanges.Core.Table;

namespace PokerRanges.Core.Postflop;

public interface IRangeAssigner
{
    IReadOnlyList<OpponentRange> Assign(
        HandState state,
        OpponentProfile profile,
        PostflopBudget budget,
        CancellationToken cancellationToken);
}
