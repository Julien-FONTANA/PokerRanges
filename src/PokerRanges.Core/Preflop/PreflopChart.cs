using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Preflop;

/// <summary>
/// One action of a chart and the range that plays it, in standard notation. Partial weights
/// ("AKo:0.5") express mixed strategies; whatever is listed nowhere is a fold.
/// </summary>
public sealed record ChartAction
{
    public required ChartActionKind Kind { get; init; }

    public double SizeInBigBlinds { get; init; }

    public required string Range { get; init; }
}

/// <summary>
/// A preflop chart. It is indexed not by a position label but by how many players act after the
/// hero: opening with three players behind poses the same problem at a five-handed table and at an
/// eight-handed one, which cuts the amount of data to write accordingly.
/// <para>
/// Known exception: the small blind has only one player behind but acts first on every postflop
/// round. Its range is therefore tighter than the button's, against the general trend — that is
/// data to be written down, not an anomaly to be corrected.
/// </para>
/// </summary>
public sealed record PreflopChart
{
    public required PreflopContext Context { get; init; }

    public required int PlayersLeftToAct { get; init; }

    public FacingRelation? Relation { get; init; }

    public required double DepthInBigBlinds { get; init; }

    public required IReadOnlyList<ChartAction> Actions { get; init; }

    /// <summary>Where this range comes from: displayed so the advice stays auditable.</summary>
    public string Source { get; init; } = string.Empty;

    public string Describe()
    {
        string relation = Relation is null ? string.Empty : $" {PreflopContextLabels.Describe(Relation.Value)},";

        return PreflopText.ChartSummary(
            PreflopContextLabels.Describe(Context),
            relation,
            PlayersLeftToAct,
            DepthInBigBlinds);
    }
}
