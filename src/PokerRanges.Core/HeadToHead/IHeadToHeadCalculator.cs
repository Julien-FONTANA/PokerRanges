namespace PokerRanges.Core.HeadToHead;

public interface IHeadToHeadCalculator
{
    Task<HeadToHeadResult> ComputeAsync(HeadToHeadRequest request, CancellationToken cancellationToken);
}
