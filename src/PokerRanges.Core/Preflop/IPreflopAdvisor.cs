using PokerRanges.Core.Table;

namespace PokerRanges.Core.Preflop;

public interface IPreflopAdvisor
{
    /// <summary>The chart that applies to this situation, regardless of the hero's hand.</summary>
    ChartResolution ResolveChart(HandState state);

    /// <summary>The advice for the hero's hand; requires their cards to be set.</summary>
    PreflopAdvice Advise(HandState state);
}
