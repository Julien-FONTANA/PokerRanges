namespace PokerRanges.Core.Preflop;

public sealed record PreflopAdvice(
    StrategyOption Recommendation,
    IReadOnlyList<StrategyOption> Options,
    ChartResolution Chart,
    PreflopSituation Situation,
    IReadOnlyList<string> Rationale)
{
    /// <summary>
    /// Vrai quand le chart mélange plusieurs actions sur cette main : la recommandation affichée
    /// n'est alors que la plus fréquente, pas la seule jouable.
    /// </summary>
    public bool IsMixed => Options.Count(option => option.Frequency > 0.01) > 1;
}
