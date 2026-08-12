using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using PokerRanges.App.Localization;
using PokerRanges.App.Rendering;
using PokerRanges.Core.Cards;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// One clickable cell of the editable 13x13. Unlike <see cref="RangeMatrixCellViewModel"/> it is
/// mutable and carries its <see cref="Cards.HandClass"/>: the grid is an input here, not a readout,
/// and 169 cells are built once and repainted in place rather than rebuilt on every keystroke.
/// </summary>
public sealed partial class RangeGridCellViewModel : ObservableObject
{
    /// <summary>Below this a cell counts as empty; above it, as part of the range.</summary>
    private const double Tolerance = 0.001;

    /// <summary>
    /// Twenty-one shades, built once. <see cref="ActionPalette.RangeWeightBrush"/> allocates a brush
    /// per call, and repainting 169 cells on every drag of the slider would churn through them.
    /// </summary>
    private static readonly IBrush[] WeightBrushes = BuildWeightBrushes();

    /// <summary>
    /// Not the palette's fold grey: in an editable grid that colour reads as "impossible" when what
    /// is meant is "not picked".
    /// </summary>
    private static readonly SolidColorBrush EmptyBackground = new(Color.Parse("#242424"));

    private static readonly SolidColorBrush PickedBorder = new(Color.Parse("#F2C94C"));

    private static readonly SolidColorBrush PlainBorder = new(Color.Parse("#333333"));

    [ObservableProperty]
    private double _weight;

    public RangeGridCellViewModel(HandClass handClass)
    {
        HandClass = handClass;
        Label = handClass.ToString();
    }

    public HandClass HandClass { get; }

    public string Label { get; }

    public bool IsPicked => Weight > Tolerance;

    public IBrush Background => IsPicked
        ? WeightBrushes[Math.Clamp((int)Math.Round(Weight * (WeightBrushes.Length - 1)), 0, WeightBrushes.Length - 1)]
        : EmptyBackground;

    public IBrush BorderBrush => IsPicked ? PickedBorder : PlainBorder;

    public string Tooltip => UiMatrixText.CellShare(HandClass, Weight);

    /// <summary>The tooltip is a composed sentence, so it does not re-translate itself.</summary>
    public void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Tooltip));
    }

    partial void OnWeightChanged(double value)
    {
        OnPropertyChanged(nameof(IsPicked));
        OnPropertyChanged(nameof(Background));
        OnPropertyChanged(nameof(BorderBrush));
        OnPropertyChanged(nameof(Tooltip));
    }

    private static IBrush[] BuildWeightBrushes()
    {
        IBrush[] brushes = new IBrush[21];
        for (int step = 0; step < brushes.Length; step++)
        {
            brushes[step] = ActionPalette.RangeWeightBrush((double)step / (brushes.Length - 1));
        }

        return brushes;
    }
}
