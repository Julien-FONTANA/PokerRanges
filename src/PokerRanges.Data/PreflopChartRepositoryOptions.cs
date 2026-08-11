namespace PokerRanges.Data;

public sealed record PreflopChartRepositoryOptions
{
    /// <summary>
    /// Directory of charts the user can edit. A chart in there carrying the same key as an embedded
    /// one replaces it; leave empty to use only the shipped charts.
    /// </summary>
    public string? UserChartsDirectory { get; init; }

    public static PreflopChartRepositoryOptions EmbeddedOnly { get; } = new();
}
