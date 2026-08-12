using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using PokerRanges.App.Localization;
using PokerRanges.App.Rendering;
using PokerRanges.Core.HeadToHead;
using PokerRanges.Core.Preflop;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// What the head-to-head calculation produced. Deliberately not
/// <see cref="RecommendationViewModel"/>: that one only knows how to read a
/// <see cref="Core.Postflop.PostflopAdvice"/>, and its trimmed evaluation list exists to feed the
/// compact window.
/// </summary>
public sealed partial class HeadToHeadResultViewModel : ObservableObject
{
    [ObservableProperty]
    private string _headline = UiText.Current.Waiting;

    [ObservableProperty]
    private IBrush _headlineBrush = ActionPalette.BrushOf(ChartActionKind.Fold);

    [ObservableProperty]
    private string _equity = string.Empty;

    [ObservableProperty]
    private string _winTieLose = string.Empty;

    [ObservableProperty]
    private string _contestedPot = string.Empty;

    [ObservableProperty]
    private string _equityNeeded = string.Empty;

    [ObservableProperty]
    private string _breakEvenFold = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<HeadToHeadActionViewModel> _actions = [];

    [ObservableProperty]
    private IReadOnlyList<string> _rationale = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _problem;

    [ObservableProperty]
    private string? _precision;

    public UiText Text => UiText.Current;

    public bool HasProblem => Problem is not null;

    public bool HasPrecision => Precision is not null;

    public bool HasActions => Actions.Count > 0;

    public void Show(HeadToHeadResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        Headline = result.Best.Label;
        HeadlineBrush = ActionPalette.BrushOf(result.Best.Kind);
        Equity = UiHeadToHeadText.EquityHeadline(result.Hero.Equity, result.Villain.Equity);
        WinTieLose = UiHeadToHeadText.WinTieLose(result.Hero.WinRate, result.Hero.TieRate, result.Hero.LoseRate);
        ContestedPot = UiHeadToHeadText.Chips(
            result.Spot.ContestedPot,
            result.Spot.BigBlind <= 0 ? 0 : result.Spot.ContestedPot / result.Spot.BigBlind);
        EquityNeeded = UiHeadToHeadText.Percent(result.Spot.BreakEvenEquityIfCalled);
        BreakEvenFold = result.BreakEvenFoldFrequency is double frequency
            ? UiHeadToHeadText.Percent(frequency)
            : UiHeadToHeadText.NotApplicable;
        Actions = HeadToHeadActionViewModel.From(result, result.Spot.BigBlind);
        Rationale = result.Rationale;
        Precision = result.DescribePrecision();
        Problem = null;
    }

    public void ShowWaiting(string message)
    {
        Headline = UiText.Current.Waiting;
        HeadlineBrush = ActionPalette.BrushOf(ChartActionKind.Fold);
        Equity = string.Empty;
        WinTieLose = string.Empty;
        ContestedPot = string.Empty;
        EquityNeeded = string.Empty;
        BreakEvenFold = string.Empty;
        Actions = [];
        Rationale = [];
        Precision = null;
        Problem = message;
    }

    partial void OnProblemChanged(string? value)
    {
        OnPropertyChanged(nameof(HasProblem));
    }

    partial void OnPrecisionChanged(string? value)
    {
        OnPropertyChanged(nameof(HasPrecision));
    }

    partial void OnActionsChanged(IReadOnlyList<HeadToHeadActionViewModel> value)
    {
        OnPropertyChanged(nameof(HasActions));
    }
}
