namespace PokerRanges.Core.Preflop;

public interface IPreflopChartRepository
{
    IReadOnlyList<PreflopChart> Charts { get; }

    /// <summary>The editable charts directory, null when only the shipped charts are used.</summary>
    string? EditableDirectory { get; }

    ChartResolution Resolve(ChartKey key);

    void Reload();

    /// <summary>
    /// Rewrites the shipped charts over the editable directory and reloads. The safety net for
    /// hand-editing: a range can be broken without having to reinstall the application.
    /// Returns the number of files rewritten.
    /// </summary>
    int RestoreDefaults();
}
