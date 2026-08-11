using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using PokerRanges.App.Localization;
using PokerRanges.App.Rendering;
using PokerRanges.Core.Postflop;
using PokerRanges.Core.Preflop;

namespace PokerRanges.App.ViewModels;

public sealed partial class RecommendationViewModel : ObservableObject
{
    [ObservableProperty]
    private string _headline = UiText.Current.Waiting;

    [ObservableProperty]
    private IBrush _headlineBrush = ActionPalette.BrushOf(ChartActionKind.Fold);

    [ObservableProperty]
    private IReadOnlyList<StrategyOptionViewModel> _options = [];

    [ObservableProperty]
    private IReadOnlyList<ActionEvaluationViewModel> _evaluations = [];

    [ObservableProperty]
    private IReadOnlyList<string> _rationale = [];

    [ObservableProperty]
    private bool _isMixed;

    [ObservableProperty]
    private bool _isClose;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _problem;

    [ObservableProperty]
    private string? _precision;

    public UiText Text => UiText.Current;

    public bool HasProblem => Problem is not null;

    public bool HasPrecision => Precision is not null;

    public bool HasEvaluations => Evaluations.Count > 0;

    /// <summary>Le haut du tableau d'espérance, seul à tenir dans la fenêtre compacte.</summary>
    public IReadOnlyList<ActionEvaluationViewModel> TopEvaluations => [.. Evaluations.Take(4)];

    public bool HasOptions => Options.Count > 0;

    public void Show(PreflopAdvice advice)
    {
        ArgumentNullException.ThrowIfNull(advice);

        Headline = advice.Recommendation.Describe();
        HeadlineBrush = ActionPalette.BrushOf(advice.Recommendation.Kind);
        Options = [.. advice.Options
            .OrderBy(option => ActionPalette.SortOrderOf(option.Kind))
            .Select(StrategyOptionViewModel.From)];
        Evaluations = [];
        Rationale = advice.Rationale;
        IsMixed = advice.IsMixed;
        IsClose = false;
        Problem = null;
        Precision = null;
    }

    public void Show(PostflopAdvice advice, double bigBlind)
    {
        ArgumentNullException.ThrowIfNull(advice);

        Headline = advice.Best.Label;
        HeadlineBrush = ActionPalette.BrushOf(advice.Best.Kind);
        Options = [];
        Evaluations = ActionEvaluationViewModel.From(advice, bigBlind);
        Rationale = advice.Rationale;
        IsMixed = false;
        IsClose = advice.IsClose;
        Problem = advice.IsHeadsUp ? null : UiText.Current.MultiwayCaveat;
        Precision = advice.DescribePrecision();
    }

    public void ShowWaiting(string message)
    {
        Headline = UiText.Current.Waiting;
        HeadlineBrush = ActionPalette.BrushOf(ChartActionKind.Fold);
        Options = [];
        Evaluations = [];
        Rationale = [];
        IsMixed = false;
        IsClose = false;
        Problem = message;
        Precision = null;
    }

    partial void OnProblemChanged(string? value)
    {
        OnPropertyChanged(nameof(HasProblem));
    }

    partial void OnPrecisionChanged(string? value)
    {
        OnPropertyChanged(nameof(HasPrecision));
    }

    partial void OnEvaluationsChanged(IReadOnlyList<ActionEvaluationViewModel> value)
    {
        OnPropertyChanged(nameof(HasEvaluations));
        OnPropertyChanged(nameof(TopEvaluations));
    }

    partial void OnOptionsChanged(IReadOnlyList<StrategyOptionViewModel> value)
    {
        OnPropertyChanged(nameof(HasOptions));
    }
}
