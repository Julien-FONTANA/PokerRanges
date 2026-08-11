namespace PokerRanges.Core.Preflop;

public sealed record PreflopAdvice(
    StrategyOption Recommendation,
    IReadOnlyList<StrategyOption> Options,
    ChartResolution Chart,
    PreflopSituation Situation,
    IReadOnlyList<string> Rationale)
{
    /// <summary>
    /// True when the chart mixes several actions on this hand: the recommendation shown is then
    /// merely the most frequent one, not the only playable one.
    /// </summary>
    public bool IsMixed => Options.Count(option => option.Frequency > 0.01) > 1;
}
