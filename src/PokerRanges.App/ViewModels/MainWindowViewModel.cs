using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PokerRanges.App.Localization;
using PokerRanges.Core;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Postflop;
using PokerRanges.Core.Session;
using PokerRanges.Core.Table;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// Drives the hand in progress. Rather than a generic action editor, the application names who has
/// to act and offers only that player's legal actions: that is how a hand gets entered at the pace
/// it is played. Computing the advice is delegated to <see cref="AdviceCoordinator"/>; what stays
/// here is the state of the hand and saving it.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private const int DebounceMilliseconds = 150;

    /// <summary>
    /// Saving is far lazier than advising: nobody needs the resume file to keep up with typing,
    /// and shutting the application down writes anyway.
    /// </summary>
    private const int PersistDebounceMilliseconds = 1500;

    private readonly IPotEngine _potEngine;
    private readonly AdviceCoordinator _advice;
    private readonly ISessionStore _sessionStore;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly List<PlayerAction> _actions = [];

    private CancellationTokenSource? _persistPending;
    private Position? _actor;
    private Street _street = Street.Preflop;
    private string? _stateProblem;

    /// <summary>
    /// True while a hand is being put back in place. Reloading fires the same notifications as
    /// typing; without this guard the half-restored state would write itself over the very file
    /// it came from.
    /// </summary>
    private bool _isRestoring;

    [ObservableProperty]
    private OpponentProfileChoice _profile = OpponentProfileChoice.All[0];

    [ObservableProperty]
    private bool _isCompact;

    /// <summary>
    /// Orthogonal to <see cref="IsCompact"/> rather than folded into a single mode, so compact mode
    /// keeps exactly the semantics it had.
    /// </summary>
    [ObservableProperty]
    private bool _isHeadToHead;

    /// <summary>
    /// Which of the two pickers the compact grid is filling. Compact mode shows one grid and not
    /// two — a second one would push the advice out of a window meant to sit beside the table — so
    /// the grid has to be told what it is filling.
    /// </summary>
    [ObservableProperty]
    private bool _isBoardTarget;

    [ObservableProperty]
    private string _turnLabel = string.Empty;

    [ObservableProperty]
    private string _handSummary = string.Empty;

    [ObservableProperty]
    private string _potLabel = string.Empty;

    [ObservableProperty]
    private string _streetLabel = TableText.StreetPreflop;

    [ObservableProperty]
    private string _callLabel = UiText.Current.Call;

    [ObservableProperty]
    private decimal _raiseAmount;

    [ObservableProperty]
    private bool _canFold;

    [ObservableProperty]
    private bool _canCheck;

    [ObservableProperty]
    private bool _canCall;

    [ObservableProperty]
    private bool _canRaise;

    [ObservableProperty]
    private bool _canUndo;

    public MainWindowViewModel(
        IPotEngine potEngine,
        AdviceCoordinator advice,
        ISessionStore sessionStore,
        ChartsViewModel charts,
        JournalViewModel journal,
        HeadToHeadViewModel headToHead,
        ILogger<MainWindowViewModel> logger)
    {
        _potEngine = potEngine;
        _advice = advice;
        _sessionStore = sessionStore;
        _logger = logger;

        Charts = charts;
        Charts.Changed += OnChartsChanged;

        Journal = journal;
        Journal.ReplayRequested += OnReplayRequested;

        // Assigned before the session is restored: restoring rebuilds the labels, which walks the
        // child view models.
        HeadToHead = headToHead;
        HeadToHead.CloseRequested += OnHeadToHeadClosed;
        HeadToHead.LanguageToggleRequested += OnHeadToHeadLanguageToggled;

        RestoreSession();

        Table.PropertyChanged += OnTableChanged;
        Hero.PropertyChanged += OnHeroChanged;
        Board.PropertyChanged += OnBoardChanged;

        ScheduleRefresh();
        _logger.LogInformation("Main window ready");
    }

    public UiText Text => UiText.Current;

    public string Title => UiText.Current.WindowTitle;

    public TableSettingsViewModel Table { get; } = new();

    public CardPickerViewModel Hero { get; } = new(2, () => UiText.Current.NoHand);

    public CardPickerViewModel Board { get; } = new(5, () => UiText.Current.EmptyBoard);

    /// <summary>The picker the single compact grid is pointed at.</summary>
    public CardPickerViewModel ActivePicker => IsBoardTarget ? Board : Hero;

    public RangeMatrixViewModel Matrix => _advice.Matrix;

    public RecommendationViewModel Recommendation => _advice.Recommendation;

    public ObservableCollection<RecordedActionViewModel> History { get; } = [];

    public ChartsViewModel Charts { get; }

    public JournalViewModel Journal { get; }

    public HeadToHeadViewModel HeadToHead { get; }

    /// <summary>
    /// The three modes are two flags rather than an enum so that compact mode's own binding is left
    /// untouched. There is no boolean "and" in the binding grammar, hence a property per panel.
    /// </summary>
    public bool ShowAnalysis => !IsCompact && !IsHeadToHead;

    public bool ShowHeadToHead => !IsCompact && IsHeadToHead;

    public IReadOnlyList<OpponentProfileChoice> Profiles => OpponentProfileChoice.All;

    /// <summary>
    /// At the table, an answer in one second beats an exact answer in five: compact mode cuts the
    /// sampling budget, and the advice then states the precision it reached.
    /// </summary>
    public PostflopBudget Budget => IsCompact ? PostflopBudget.Fast : PostflopBudget.Full;

    public string ShortcutsLabel => UiText.Current.Shortcuts;

    /// <summary>
    /// Switches between the two languages. The whole display is re-read — down to the engine's
    /// sentences, rebuilt by the recomputation — with no restart and without losing the hand.
    /// </summary>
    [RelayCommand]
    public void ToggleLanguage()
    {
        Language.Use(Language.IsFrench ? AppLanguage.English : AppLanguage.French);

        _logger.LogInformation("Interface language: {Language}", Language.Current);

        RefreshLabels();
        ScheduleRefresh();
    }

    [RelayCommand]
    public void ToggleCompact()
    {
        IsCompact = !IsCompact;
    }

    /// <summary>
    /// Opens the head-to-head calculator, seeded from the hand in progress. Compact mode is left
    /// behind on the way in: the shortcut fires there too, and leaving both flags raised would send
    /// the next F2 somewhere the user did not ask for.
    /// </summary>
    [RelayCommand]
    public void ToggleHeadToHead()
    {
        IsHeadToHead = !IsHeadToHead;

        if (!IsHeadToHead)
        {
            return;
        }

        IsCompact = false;

        if (_stateProblem is not null)
        {
            // The hand on screen does not add up, so there is nothing worth copying across. The
            // panel keeps its own settings and is usable on its own.
            _ = HeadToHead.ComputeNowAsync();
            return;
        }

        HandState state = BuildState();
        HeadToHead.SeedFrom(state, _potEngine.Analyse(state));
    }

    [RelayCommand]
    public void TargetHand()
    {
        IsBoardTarget = false;
    }

    [RelayCommand]
    public void TargetBoard()
    {
        IsBoardTarget = true;
    }

    /// <summary>Empties the picker the compact grid is filling, and only that one.</summary>
    [RelayCommand]
    public void ClearActivePicker()
    {
        ActivePicker.Clear();
    }

    [RelayCommand]
    public void Fold()
    {
        RecordForCurrentActor(PlayerAction.Fold);
    }

    [RelayCommand]
    public void Check()
    {
        RecordForCurrentActor(PlayerAction.Check);
    }

    [RelayCommand]
    public void Call()
    {
        RecordForCurrentActor(PlayerAction.Call);
    }

    [RelayCommand]
    public void Raise()
    {
        if (_actor is not Position actor)
        {
            return;
        }

        Record(_street == Street.Preflop || CanCall
            ? PlayerAction.RaiseTo(_street, actor, (double)RaiseAmount)
            : PlayerAction.BetTo(_street, actor, (double)RaiseAmount));
    }

    [RelayCommand]
    public void Undo()
    {
        if (_actions.Count == 0)
        {
            return;
        }

        _actions.RemoveAt(_actions.Count - 1);
        History.RemoveAt(History.Count - 1);
        ScheduleRefresh();
    }

    /// <summary>
    /// Starting a hand archives the previous one: it is the only moment the application knows for
    /// certain that a hand is over, since nothing tells it the result at showdown.
    /// </summary>
    [RelayCommand]
    public void NewHand()
    {
        ArchiveCurrentHand();

        _actions.Clear();
        History.Clear();
        Hero.Clear();
        Board.Clear();
        IsBoardTarget = false;
        ScheduleRefresh();
    }

    /// <summary>
    /// Recomputes without waiting for the debounce, cancelling the request in flight. Used by the
    /// tests and by any explicit trigger.
    /// </summary>
    public async Task RefreshNowAsync()
    {
        ApplyState();
        await RunAdviceAsync(0);
    }

    /// <summary>Writes without waiting for the debounce. Called when the application closes.</summary>
    public void PersistNow()
    {
        _persistPending?.Cancel();
        Persist();
    }

    partial void OnProfileChanged(OpponentProfileChoice value)
    {
        ScheduleRefresh();
    }

    partial void OnIsHeadToHeadChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowAnalysis));
        OnPropertyChanged(nameof(ShowHeadToHead));
        HeadToHead.IsActive = value;
    }

    partial void OnIsCompactChanged(bool value)
    {
        OnPropertyChanged(nameof(Budget));
        OnPropertyChanged(nameof(ShowAnalysis));
        OnPropertyChanged(nameof(ShowHeadToHead));
        _logger.LogInformation("Switched to {Mode} mode", value ? "compact" : "analysis");
        ScheduleRefresh();
    }

    partial void OnIsBoardTargetChanged(bool value)
    {
        OnPropertyChanged(nameof(ActivePicker));
    }

    private void OnChartsChanged(object? sender, EventArgs args)
    {
        ScheduleRefresh();
    }

    private void OnTableChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(TableSettingsViewModel.AvailablePositions)
            || args.PropertyName == nameof(TableSettingsViewModel.DepthLabel))
        {
            return;
        }

        _actions.Clear();
        History.Clear();
        ScheduleRefresh();
    }

    private void OnHeroChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(CardPickerViewModel.Selection))
        {
            return;
        }

        // Once the hand is complete the hero picker has nothing left to take, so the compact grid
        // moves itself on to the board: entering a hand is then one uninterrupted run of clicks.
        if (Hero.Selection.Count == Hero.Capacity)
        {
            IsBoardTarget = true;
        }

        Board.SetUnavailable([.. Hero.Selection]);
        ScheduleRefresh();
    }

    private void OnBoardChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(CardPickerViewModel.Selection))
        {
            return;
        }

        Hero.SetUnavailable([.. Board.Selection]);
        ScheduleRefresh();
    }

    private void RecordForCurrentActor(Func<Street, Position, PlayerAction> build)
    {
        if (_actor is Position actor)
        {
            Record(build(_street, actor));
        }
    }

    private void Record(PlayerAction action)
    {
        _actions.Add(action);
        History.Add(RecordedActionViewModel.From(action, Table.HeroPosition?.Value ?? Position.BigBlind));
        ScheduleRefresh();
    }

    /// <summary>
    /// The betting state is recomputed immediately — otherwise two quick clicks would record the
    /// action against the wrong player — and only the advice is deferred.
    /// </summary>
    private void ScheduleRefresh()
    {
        ApplyState();
        _ = RunAdviceAsync(DebounceMilliseconds);
        SchedulePersist();
    }

    private Task RunAdviceAsync(int delayMilliseconds)
    {
        if (_stateProblem is string problem)
        {
            _advice.ShowProblem(problem);
            return Task.CompletedTask;
        }

        HandState state = BuildState();

        return _advice.ShowAsync(
            new AdviceRequest
            {
                State = state,
                Analysis = _potEngine.Analyse(state),
                Profile = Profile.Value,
                Budget = Budget,
            },
            delayMilliseconds);
    }

    private void ApplyState()
    {
        _stateProblem = null;
        RefreshHandSummary();

        try
        {
            if (Board.Selection.Count is 1 or 2)
            {
                _stateProblem = UiMatrixText.IncompleteBoard(Board.Selection.Count);
                DisableActions();
                return;
            }

            HandState state = BuildState();
            HandAnalysis analysis = _potEngine.Analyse(state);
            _street = analysis.Street;
            UpdateBettingControls(analysis, state.Table);
        }
        catch (PokerRangesException exception)
        {
            _logger.LogWarning(exception, "Invalid hand: {Message}", exception.Message);
            _stateProblem = exception.Message;
            DisableActions();
        }
        finally
        {
            CanUndo = _actions.Count > 0;
        }
    }

    /// <summary>
    /// Labels already computed do not re-translate themselves: they are rebuilt here, while the
    /// fixed XAML labels follow <see cref="UiText"/>'s notification.
    /// </summary>
    private void RefreshLabels()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(ShortcutsLabel));

        Table.Refresh();
        Charts.Refresh();
        Journal.Refresh();
        Hero.RefreshLabel();
        Board.RefreshLabel();
        HeadToHead.Refresh();
        RebuildHistory();
    }

    private void OnHeadToHeadClosed(object? sender, EventArgs args)
    {
        IsHeadToHead = false;
    }

    private void OnHeadToHeadLanguageToggled(object? sender, EventArgs args)
    {
        ToggleLanguage();
    }

    /// <summary>
    /// The history is made of sentences written at the moment of the action: they do not
    /// re-translate, they must be rewritten from the actions, the only thing that was kept.
    /// </summary>
    private void RebuildHistory()
    {
        Position hero = Table.HeroPosition?.Value ?? Position.BigBlind;

        History.Clear();

        foreach (PlayerAction action in _actions)
        {
            History.Add(RecordedActionViewModel.From(action, hero));
        }
    }

    /// <summary>
    /// The context line of compact mode. Cut down to the recommendation, the display no longer
    /// shows the settings: it must at least recall which table it is reasoning about.
    /// </summary>
    private void RefreshHandSummary()
    {
        HandSummary = string.Join(
            " · ",
            UiMatrixText.PlayerCount(Table.PlayerCount),
            Table.HeroPosition?.Label ?? PositionLayout.Describe(Position.BigBlind),
            Table.DepthLabel,
            Hero.Selection.Count == 2 ? Hero.Label : UiMatrixText.HandToEnter,
            Board.Selection.Count == 0 ? SessionText.BeforeTheFlop : Board.Label);
    }

    private HandState BuildState()
    {
        return new HandState
        {
            Table = Table.Build(),
            HeroCards = Hero.AsHoleCards,
            Board = [.. Board.Selection],
            Actions = [.. _actions],
        };
    }

    private void UpdateBettingControls(HandAnalysis analysis, TableConfiguration table)
    {
        StreetLabel = TableText.Describe(analysis.Street);
        PotLabel = UiMatrixText.Pot(analysis.Pot, analysis.Pot / table.BigBlind);

        _actor = analysis.NextToAct;

        if (_actor is not Position actor)
        {
            TurnLabel = UiMatrixText.BettingRoundOver(TableText.Describe(analysis.Street));
            DisableActions();
            return;
        }

        PotSnapshot snapshot = analysis.For(actor);

        TurnLabel = actor == table.HeroPosition
            ? UiMatrixText.YourTurn(PositionLayout.Describe(actor))
            : UiMatrixText.TheirTurn(PositionLayout.Describe(actor));

        CanFold = true;
        CanCheck = !snapshot.IsFacingABet;
        CanCall = snapshot.IsFacingABet;
        CanRaise = snapshot.RemainingStack > snapshot.AmountToCall;
        CallLabel = snapshot.IsFacingABet
            ? UiMatrixText.CallAmount(snapshot.AmountToCall)
            : UiText.Current.Call;
        RaiseAmount = SuggestRaise(analysis, snapshot, table);
    }

    private static decimal SuggestRaise(HandAnalysis analysis, PotSnapshot snapshot, TableConfiguration table)
    {
        double target = analysis.CurrentBet > 0
            ? Math.Max(analysis.CurrentBet * 2.5, table.BigBlind * 2.2)
            : analysis.Pot * 0.5;

        return Math.Round((decimal)Math.Min(target, snapshot.MaximumRaiseTo), 2);
    }

    private void DisableActions()
    {
        CanFold = false;
        CanCheck = false;
        CanCall = false;
        CanRaise = false;
        _actor = null;
    }

    private void RestoreSession()
    {
        _isRestoring = true;

        try
        {
            UserPreferences preferences = _sessionStore.LoadPreferences();

            // Language first: everything that follows produces text, and would produce it in the
            // wrong language if the order were reversed.
            Language.Use(preferences.Language);

            Table.Apply(preferences);
            Profile = OpponentProfileChoice.Of(OpponentProfile.Find(preferences.OpponentProfile));
            IsCompact = preferences.PrefersCompactLayout;

            if (_sessionStore.LoadHand() is HandState hand)
            {
                Load(hand);
                _logger.LogInformation("Hand in progress resumed: {Count} action(s).", hand.Actions.Count);
            }

            // Labels computed at construction were computed before the saved language was known:
            // without this re-read, an unchanged setting would keep its English wording.
            RefreshLabels();
        }
        finally
        {
            _isRestoring = false;
        }
    }

    private void OnReplayRequested(object? sender, JournalEntry entry)
    {
        ArchiveCurrentHand();

        _isRestoring = true;

        try
        {
            Load(entry.Hand);
        }
        finally
        {
            _isRestoring = false;
        }

        ScheduleRefresh();
    }

    /// <summary>
    /// Puts a whole hand back in place. Both pickers are cleared before being filled: otherwise a
    /// board card would still be held by the previous hand and would be refused.
    /// </summary>
    private void Load(HandState hand)
    {
        Table.Apply(ToPreferences(hand.Table, (double)Table.AnteAmount));

        Hero.Restore([]);
        Board.Restore([]);
        Hero.Restore(hand.HeroCards is HoleCards cards ? [cards.First, cards.Second] : []);
        Board.Restore(hand.Board);

        // What each picker forbids the other is normally recomputed by their notifications, which
        // are not yet subscribed while the session is being restored: a hand resumed at startup
        // would otherwise offer its own board cards as hole cards. The compact grid, pointed at
        // the picker still to be filled, follows the same reasoning.
        Board.SetUnavailable([.. Hero.Selection]);
        Hero.SetUnavailable([.. Board.Selection]);
        IsBoardTarget = Hero.Selection.Count == Hero.Capacity;

        _actions.Clear();
        History.Clear();

        foreach (PlayerAction action in hand.Actions)
        {
            _actions.Add(action);
            History.Add(RecordedActionViewModel.From(action, hand.Table.HeroPosition));
        }
    }

    /// <summary>
    /// The interface can only set a uniform stack, so resuming a hand loses nothing: it could
    /// never have produced uneven ones. The ante amount, though, is taken from the display when
    /// the hand was played without antes: an ante-free hand carries a zero amount, and applying
    /// it would wipe the setting the user will want back at the next table.
    /// </summary>
    private static UserPreferences ToPreferences(TableConfiguration table, double anteWhenDisabled)
    {
        return new UserPreferences
        {
            PlayerCount = table.PlayerCount,
            BigBlind = table.BigBlind,
            StartingStack = table.StackOf(table.HeroPosition),
            AnteStyle = table.AnteStyle,
            AnteAmount = table.AnteStyle == AnteStyle.None ? anteWhenDisabled : table.AnteAmount,
            HeroPosition = table.HeroPosition,
        };
    }

    private void ArchiveCurrentHand()
    {
        if (_stateProblem is not null)
        {
            return;
        }

        Journal.Record(BuildState(), Recommendation.Headline, Recommendation.Rationale);
    }

    private void SchedulePersist()
    {
        if (_isRestoring)
        {
            return;
        }

        _persistPending?.Cancel();
        CancellationTokenSource current = new();
        _persistPending = current;

        _ = PersistAfterAsync(current.Token);
    }

    private async Task PersistAfterAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(PersistDebounceMilliseconds, cancellationToken);
            Persist();
        }
        catch (OperationCanceledException)
        {
            // A more recent entry will write in its place.
        }
    }

    private void Persist()
    {
        _sessionStore.SavePreferences(Table.Capture() with
        {
            OpponentProfile = Profile.Label,
            PrefersCompactLayout = IsCompact,
            Language = Language.Current,
        });

        _sessionStore.SaveHand(_actions.Count == 0 && Hero.Selection.Count == 0 ? null : BuildState());
    }
}
