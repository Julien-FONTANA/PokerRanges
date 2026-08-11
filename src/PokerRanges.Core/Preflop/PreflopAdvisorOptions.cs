namespace PokerRanges.Core.Preflop;

public sealed record PreflopAdvisorOptions
{
    /// <summary>
    /// Below this depth we switch to the shove charts: raising small stops making sense once the
    /// raise already commits most of the stack.
    /// </summary>
    public double JamThresholdInBigBlinds { get; init; } = 15;

    public static PreflopAdvisorOptions Default { get; } = new();
}
