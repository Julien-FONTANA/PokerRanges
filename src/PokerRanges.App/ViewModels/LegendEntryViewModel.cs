using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using PokerRanges.App.Localization;
using PokerRanges.App.Rendering;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Preflop;

namespace PokerRanges.App.ViewModels;

public sealed class LegendEntryViewModel : ObservableObject
{
    private readonly Func<string> _text;

    private LegendEntryViewModel(Func<string> text, IBrush brush)
    {
        _text = text;
        Brush = brush;

        Language.Changed += (_, _) => OnPropertyChanged(nameof(Label));
    }

    public IBrush Brush { get; }

    /// <summary>Re-read on every display: the legend follows the language without a rebuild.</summary>
    public string Label => _text();

    /// <summary>The preflop grid: each colour is one action from the chart.</summary>
    public static IReadOnlyList<LegendEntryViewModel> Actions { get; } =
    [
        new(() => UiText.Current.LegendJam, ActionPalette.BrushOf(ChartActionKind.Jam)),
        new(() => UiText.Current.LegendRaise, ActionPalette.BrushOf(ChartActionKind.Raise)),
        new(() => UiText.Current.LegendCall, ActionPalette.BrushOf(ChartActionKind.Call)),
        new(() => UiText.Current.LegendFold, ActionPalette.BrushOf(ChartActionKind.Fold)),
    ];

    /// <summary>
    /// The postflop grid: colour is no longer an action but a quantity — the share of the cell the
    /// opponent can still hold. Reusing the action legend here would read as "they fold" where it
    /// must read "they cannot have this hand".
    /// </summary>
    public static IReadOnlyList<LegendEntryViewModel> RangeWeights { get; } =
    [
        new(() => UiText.Current.LegendAllCombos, ActionPalette.RangeWeightBrush(1)),
        new(() => UiText.Current.LegendHalfCombos, ActionPalette.RangeWeightBrush(0.5)),
        new(() => UiText.Current.LegendNoCombos, ActionPalette.RangeWeightBrush(0)),
    ];
}
