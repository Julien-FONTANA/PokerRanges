using PokerRanges.Core.Table;

namespace PokerRanges.Core.Postflop;

public interface IPostflopAdvisor
{
    Task<PostflopAdvice> AdviseAsync(
        HandState state,
        OpponentProfile profile,
        PostflopBudget budget,
        CancellationToken cancellationToken);
}
