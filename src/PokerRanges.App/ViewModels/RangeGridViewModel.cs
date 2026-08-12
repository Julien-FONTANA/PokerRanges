using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PokerRanges.App.Localization;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Ranges;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// One side's range, editable three ways at once: click the grid, type the notation, or drag the
/// strongest-percent slider. The range itself is the single source of truth; the three views are
/// kept in step around it.
/// </summary>
public sealed partial class RangeGridViewModel : ObservableObject
{
    private readonly IPreflopHandStrength _strength;
    private readonly RangeGridCellViewModel[] _cells;

    /// <summary>
    /// True while one view is updating the others. Without it, rewriting the text on every keystroke
    /// would reorder what the user is halfway through typing — the writer normalises "AKo, QQ" into
    /// "QQ, AKo".
    /// </summary>
    private bool _isSyncing;

    [ObservableProperty]
    private string _notation = string.Empty;

    [ObservableProperty]
    private string? _notationError;

    [ObservableProperty]
    private double _topPercent;

    [ObservableProperty]
    private string _summary = string.Empty;

    public RangeGridViewModel(IPreflopHandStrength strength)
    {
        _strength = strength;
        _cells = [.. HandClass.All.Select(handClass => new RangeGridCellViewModel(handClass))];
        Range = HandRange.Empty;
        Summary = UiHeadToHeadText.RangeSummary(0, 0);
    }

    /// <summary>Raised whenever the range changes, whichever of the three inputs did it.</summary>
    public event EventHandler? Changed;

    public UiText Text => UiText.Current;

    public IReadOnlyList<RangeGridCellViewModel> Cells => _cells;

    public HandRange Range { get; private set; }

    public bool HasNotationError => NotationError is not null;

    /// <summary>Replaces the range from outside — a prefill, a swap, a preset.</summary>
    public void Set(HandRange range)
    {
        Apply(range, rewriteNotation: true);
    }

    /// <summary>Rewrites the computed label after a language change.</summary>
    public void Refresh()
    {
        Summary = UiHeadToHeadText.RangeSummary(Range.TotalCombos, Range.PercentOfAllHands);

        foreach (RangeGridCellViewModel cell in _cells)
        {
            cell.OnLanguageChanged();
        }
    }

    [RelayCommand]
    public void Toggle(RangeGridCellViewModel? cell)
    {
        if (cell is null)
        {
            return;
        }

        double weight = cell.IsPicked ? 0 : 1;

        // Rebuilt from the range rather than from the cells, so a range typed combo by combo
        // ("AhKh:0.5") keeps that detail everywhere the click did not land.
        HandRangeBuilder builder = new();
        foreach (WeightedCombo combo in Range.EnumerateCombos())
        {
            builder.Set(combo.Combo, combo.Weight);
        }

        builder.Set(cell.HandClass, weight);

        Apply(builder.Build(), rewriteNotation: true);
    }

    [RelayCommand]
    public void Clear()
    {
        Apply(HandRange.Empty, rewriteNotation: true);
    }

    partial void OnNotationChanged(string value)
    {
        if (_isSyncing)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            NotationError = null;
            Apply(HandRange.Empty, rewriteNotation: false);
            return;
        }

        HandRange parsed;
        try
        {
            parsed = RangeNotationParser.Parse(value);
        }
        catch (RangeNotationException exception)
        {
            // Half a token is a keystroke in progress, not a mistake: keep the range we had and let
            // the user finish typing.
            NotationError = exception.Message;
            return;
        }

        NotationError = null;
        Apply(parsed, rewriteNotation: false);
    }

    /// <summary>
    /// The slider writes into the range and is never written back from it: recomputing it whenever
    /// the range changed would make a single click on the grid snap the slider, re-derive a whole
    /// percentile and quietly discard the edit.
    /// </summary>
    partial void OnTopPercentChanged(double value)
    {
        if (_isSyncing)
        {
            return;
        }

        Apply(_strength.TopPercent(value), rewriteNotation: true);
    }

    partial void OnNotationErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HasNotationError));
    }

    private void Apply(HandRange range, bool rewriteNotation)
    {
        Range = range;

        foreach (RangeGridCellViewModel cell in _cells)
        {
            cell.Weight = range.FrequencyOf(cell.HandClass);
        }

        if (rewriteNotation)
        {
            _isSyncing = true;
            try
            {
                Notation = RangeNotationWriter.Write(range);
                NotationError = null;
            }
            finally
            {
                _isSyncing = false;
            }
        }

        Summary = UiHeadToHeadText.RangeSummary(range.TotalCombos, range.PercentOfAllHands);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
