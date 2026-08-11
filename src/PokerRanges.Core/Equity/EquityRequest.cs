using PokerRanges.Core.Cards;
using PokerRanges.Core.Ranges;

namespace PokerRanges.Core.Equity;

/// <summary>
/// A request for an equity calculation. By convention the player at index 0 is the hero: it is
/// their equity that drives the convergence criterion.
/// </summary>
public sealed record EquityRequest
{
    public required IReadOnlyList<HandRange> PlayerRanges { get; init; }

    public IReadOnlyList<Card> Board { get; init; } = [];

    public IReadOnlyList<Card> DeadCards { get; init; } = [];

    public EquityMethod Method { get; init; } = EquityMethod.Automatic;

    /// <summary>Sample ceiling for Monte-Carlo. No effect on exhaustive enumeration.</summary>
    public int MaximumSamples { get; init; } = 200_000;

    /// <summary>
    /// Target standard error on the hero's equity; sampling stops as soon as it is reached.
    /// 0.0015 is roughly ± 0.15 of an equity point.
    /// </summary>
    public double TargetStandardError { get; init; } = 0.0015;

    /// <summary>
    /// Fixed seed: makes sampling reproducible by forcing a single thread of execution.
    /// Reserved for tests and diagnostics.
    /// </summary>
    public int? RandomSeed { get; init; }

    public static EquityRequest Between(params HandRange[] playerRanges)
    {
        return new EquityRequest { PlayerRanges = playerRanges };
    }
}
