namespace PokerRanges.Core.Equity;

public interface IEquityCalculator
{
    Task<EquityResult> ComputeAsync(EquityRequest request, CancellationToken cancellationToken);
}
