using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Preflop;

/// <summary>
/// Le chart réellement utilisé pour répondre, et la liste des écarts entre ce qui était demandé et
/// ce qui existait. Aucun conseil ne doit sortir de l'application sans que l'on puisse remonter à
/// la donnée qui l'a produit.
/// </summary>
public sealed record ChartResolution(
    PreflopChart Chart,
    ChartKey Requested,
    RangeStrategy Strategy,
    IReadOnlyList<string> Adjustments)
{
    public bool IsExactMatch => Adjustments.Count == 0;

    public string Describe()
    {
        return IsExactMatch
            ? PreflopText.ChartPrefix(Chart.Describe())
            : PreflopText.ChartPrefixWithAdjustments(
                Chart.Describe(),
                string.Join(PreflopText.AdjustmentSeparator, Adjustments));
    }
}
