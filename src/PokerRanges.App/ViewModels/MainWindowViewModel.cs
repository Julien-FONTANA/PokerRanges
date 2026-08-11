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
/// Pilote la main en cours. Plutôt qu'un éditeur d'actions générique, l'application désigne qui
/// doit parler et ne propose que les actions légales de ce joueur : c'est ainsi qu'on saisit une
/// main au rythme où elle se déroule. Le calcul du conseil est délégué à
/// <see cref="AdviceCoordinator"/> ; ce qui reste ici, c'est l'état de la main et son enregistrement.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private const int DebounceMilliseconds = 150;

    /// <summary>
    /// L'enregistrement est bien plus paresseux que le conseil : personne n'a besoin que le fichier
    /// de reprise suive la frappe au clavier, et l'arrêt de l'application écrit de toute façon.
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
    /// Vrai pendant qu'on remet une main en place. Recharger déclenche les mêmes notifications
    /// qu'une saisie ; sans ce garde-fou l'état à moitié restauré s'écrirait par-dessus le fichier
    /// dont il sort.
    /// </summary>
    private bool _isRestoring;

    [ObservableProperty]
    private OpponentProfileChoice _profile = OpponentProfileChoice.All[0];

    [ObservableProperty]
    private bool _isCompact;

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

        RestoreSession();

        Table.PropertyChanged += OnTableChanged;
        Hero.PropertyChanged += OnHeroChanged;
        Board.PropertyChanged += OnBoardChanged;

        ScheduleRefresh();
        _logger.LogInformation("Fenêtre principale prête");
    }

    public UiText Text => UiText.Current;

    public string Title => UiText.Current.WindowTitle;

    public TableSettingsViewModel Table { get; } = new();

    public CardPickerViewModel Hero { get; } = new(2, () => UiText.Current.NoHand);

    public CardPickerViewModel Board { get; } = new(5, () => UiText.Current.EmptyBoard);

    public RangeMatrixViewModel Matrix => _advice.Matrix;

    public RecommendationViewModel Recommendation => _advice.Recommendation;

    public ObservableCollection<RecordedActionViewModel> History { get; } = [];

    public ChartsViewModel Charts { get; }

    public JournalViewModel Journal { get; }

    public IReadOnlyList<OpponentProfileChoice> Profiles => OpponentProfileChoice.All;

    /// <summary>
    /// À la table, une réponse en une seconde vaut mieux qu'une réponse exacte en cinq : le mode
    /// compact réduit le budget de tirages, et l'avis affiche alors la précision qu'il a atteinte.
    /// </summary>
    public PostflopBudget Budget => IsCompact ? PostflopBudget.Fast : PostflopBudget.Full;

    public string ShortcutsLabel => UiText.Current.Shortcuts;

    /// <summary>
    /// Bascule entre les deux langues. Tout l'affichage se relit — jusqu'aux phrases du moteur, qui
    /// sont reconstruites par le recalcul — sans qu'il faille redémarrer ni perdre la main en cours.
    /// </summary>
    [RelayCommand]
    public void ToggleLanguage()
    {
        Language.Use(Language.IsFrench ? AppLanguage.English : AppLanguage.French);

        _logger.LogInformation("Langue de l'interface : {Language}", Language.Current);

        RefreshLabels();
        ScheduleRefresh();
    }

    [RelayCommand]
    public void ToggleCompact()
    {
        IsCompact = !IsCompact;
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
    /// Commencer une main archive la précédente : c'est le seul moment où l'application sait avec
    /// certitude qu'une main est finie, puisque rien ne l'informe du résultat au tapis.
    /// </summary>
    [RelayCommand]
    public void NewHand()
    {
        ArchiveCurrentHand();

        _actions.Clear();
        History.Clear();
        Hero.Clear();
        Board.Clear();
        ScheduleRefresh();
    }

    /// <summary>
    /// Recalcule sans attendre l'anti-rebond, en annulant la requête en cours. Utilisé par les
    /// tests et par tout déclenchement explicite.
    /// </summary>
    public async Task RefreshNowAsync()
    {
        ApplyState();
        await RunAdviceAsync(0);
    }

    /// <summary>Écrit sans attendre l'anti-rebond. Appelé à la fermeture de l'application.</summary>
    public void PersistNow()
    {
        _persistPending?.Cancel();
        Persist();
    }

    partial void OnProfileChanged(OpponentProfileChoice value)
    {
        ScheduleRefresh();
    }

    partial void OnIsCompactChanged(bool value)
    {
        OnPropertyChanged(nameof(Budget));
        _logger.LogInformation("Bascule en mode {Mode}", value ? "compact" : "analyse");
        ScheduleRefresh();
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
    /// L'état des enchères est recalculé immédiatement — sinon deux clics rapprochés
    /// enregistreraient l'action du mauvais joueur — et seul le conseil part en différé.
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
            _logger.LogWarning(exception, "Main invalide : {Message}", exception.Message);
            _stateProblem = exception.Message;
            DisableActions();
        }
        finally
        {
            CanUndo = _actions.Count > 0;
        }
    }

    /// <summary>
    /// Les libellés déjà calculés ne se retraduisent pas tout seuls : ils sont reconstruits ici,
    /// tandis que les libellés fixes du XAML suivent la notification de <see cref="UiText"/>.
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
        RebuildHistory();
    }

    /// <summary>
    /// Le déroulé est fait de phrases écrites au moment de l'action : elles ne se retraduisent pas,
    /// il faut les réécrire à partir des actions, qui sont la seule chose que l'on ait conservée.
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
    /// La ligne de contexte du mode compact. Réduit à la recommandation, l'affichage ne montre
    /// plus les réglages : il doit au moins rappeler sur quelle table il raisonne.
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

            // La langue d'abord : tout ce qui suit produit du texte, et le produirait dans la
            // mauvaise langue si l'ordre était inversé.
            Language.Use(preferences.Language);

            Table.Apply(preferences);
            Profile = OpponentProfileChoice.Of(OpponentProfile.Find(preferences.OpponentProfile));
            IsCompact = preferences.PrefersCompactLayout;

            if (_sessionStore.LoadHand() is HandState hand)
            {
                Load(hand);
                _logger.LogInformation("Main en cours reprise : {Count} action(s).", hand.Actions.Count);
            }

            // Les libellés calculés à la construction l'ont été avant que la langue enregistrée ne
            // soit connue : sans cette relecture, un réglage inchangé garderait sa phrase anglaise.
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
    /// Remet une main entière en place. Les deux sélecteurs sont vidés avant d'être remplis :
    /// autrement une carte du board resterait retenue par la main précédente et se verrait refusée.
    /// </summary>
    private void Load(HandState hand)
    {
        Table.Apply(ToPreferences(hand.Table, (double)Table.AnteAmount));

        Hero.Restore([]);
        Board.Restore([]);
        Hero.Restore(hand.HeroCards is HoleCards cards ? [cards.First, cards.Second] : []);
        Board.Restore(hand.Board);

        _actions.Clear();
        History.Clear();

        foreach (PlayerAction action in hand.Actions)
        {
            _actions.Add(action);
            History.Add(RecordedActionViewModel.From(action, hand.Table.HeroPosition));
        }
    }

    /// <summary>
    /// L'interface ne sait régler qu'un tapis uniforme : reprendre une main n'en perd donc rien,
    /// puisqu'elle n'a jamais pu en produire d'inégaux. Le montant d'ante, lui, est repris de
    /// l'affichage quand la main s'est jouée sans ante : une main sans ante porte un montant nul,
    /// et l'appliquer effacerait le réglage que l'utilisateur retrouvera à la prochaine table.
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
            // Une saisie plus récente écrira à sa place.
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
