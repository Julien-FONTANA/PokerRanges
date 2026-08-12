using PokerRanges.Core.Cards;
using PokerRanges.Core.Equity;
using PokerRanges.Core.Postflop;
using PokerRanges.Core.Ranges;

namespace PokerRanges.Core.HeadToHead;

/// <summary>Everything the calculation needs, frozen at the moment it is requested.</summary>
public sealed record HeadToHeadRequest
{
    public required HandRange HeroRange { get; init; }

    /// <summary>
    /// When the hero jams this is the range the villain <em>calls</em> with, and its size against all
    /// possible hands is what sets his fold frequency. When the hero is calling a jam it is the range
    /// he <em>jams</em> with, and no fold frequency applies.
    /// </summary>
    public required HandRange VillainRange { get; init; }

    public required HeadToHeadSpot Spot { get; init; }

    public IReadOnlyList<Card> Board { get; init; } = [];

    /// <summary>Set when the hero's side is one exact hand rather than a range.</summary>
    public HoleCards? HeroCards { get; init; }

    /// <summary>
    /// Set when the villain's side is one exact hand. Such an opponent cannot fold — he holds that
    /// hand — so the fold frequency read off a range no longer means anything.
    /// </summary>
    public HoleCards? VillainCards { get; init; }

    /// <summary>
    /// Left to <see cref="EquityMethod.Automatic"/> so a cheap spot — two known hands on a river
    /// board — is enumerated exactly rather than sampled.
    /// </summary>
    public EquityMethod Method { get; init; } = EquityMethod.Automatic;

    public int MaximumSamples { get; init; } = 200_000;

    public double TargetStandardError { get; init; } = 0.0015;

    /// <summary>
    /// Seeded so the same spot always gives the same answer, as the rest of the engine is. Setting a
    /// seed also pins the sampling to a single thread.
    /// </summary>
    public int? RandomSeed { get; init; } = PostflopOptions.Default.RandomSeed;
}
