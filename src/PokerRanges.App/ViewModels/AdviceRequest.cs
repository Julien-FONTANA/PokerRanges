using PokerRanges.Core.Postflop;
using PokerRanges.Core.Table;

namespace PokerRanges.App.ViewModels;

/// <summary>Everything the advice needs, frozen at the moment it is requested.</summary>
public sealed record AdviceRequest
{
    public required HandState State { get; init; }

    public required HandAnalysis Analysis { get; init; }

    public required OpponentProfile Profile { get; init; }

    public required PostflopBudget Budget { get; init; }
}
