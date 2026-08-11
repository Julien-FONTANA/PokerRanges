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

    /// <summary>Relu à chaque affichage : la légende suit la langue sans être reconstruite.</summary>
    public string Label => _text();

    /// <summary>La grille préflop : chaque couleur est une action du chart.</summary>
    public static IReadOnlyList<LegendEntryViewModel> Actions { get; } =
    [
        new(() => UiText.Current.LegendJam, ActionPalette.BrushOf(ChartActionKind.Jam)),
        new(() => UiText.Current.LegendRaise, ActionPalette.BrushOf(ChartActionKind.Raise)),
        new(() => UiText.Current.LegendCall, ActionPalette.BrushOf(ChartActionKind.Call)),
        new(() => UiText.Current.LegendFold, ActionPalette.BrushOf(ChartActionKind.Fold)),
    ];

    /// <summary>
    /// La grille postflop : la couleur n'est plus une action mais une quantité — la part de la case
    /// que l'adversaire peut encore avoir. Réutiliser la légende des actions ici ferait lire « il
    /// passe » là où il faut lire « il ne peut pas avoir cette main ».
    /// </summary>
    public static IReadOnlyList<LegendEntryViewModel> RangeWeights { get; } =
    [
        new(() => UiText.Current.LegendAllCombos, ActionPalette.RangeWeightBrush(1)),
        new(() => UiText.Current.LegendHalfCombos, ActionPalette.RangeWeightBrush(0.5)),
        new(() => UiText.Current.LegendNoCombos, ActionPalette.RangeWeightBrush(0)),
    ];
}
