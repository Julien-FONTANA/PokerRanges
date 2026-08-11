using PokerRanges.Core.Postflop;
using PokerRanges.Core.Table;

namespace PokerRanges.App.ViewModels;

/// <summary>Tout ce dont le conseil a besoin, figé à l'instant où il est demandé.</summary>
public sealed record AdviceRequest
{
    public required HandState State { get; init; }

    public required HandAnalysis Analysis { get; init; }

    public required OpponentProfile Profile { get; init; }

    public required PostflopBudget Budget { get; init; }
}
