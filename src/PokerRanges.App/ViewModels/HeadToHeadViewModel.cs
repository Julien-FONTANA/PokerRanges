using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PokerRanges.App.Localization;
using PokerRanges.Core;
using PokerRanges.Core.Cards;
using PokerRanges.Core.HeadToHead;
using PokerRanges.Core.Ranges;
using PokerRanges.Core.Table;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// The head-to-head calculator: two ranges, one all-in, and what each option is worth. It is
/// seeded from the hand in progress and never writes back to it — a hypothetical spot must not be
/// able to disturb the hand actually being played.
/// </summary>
public sealed partial class HeadToHeadViewModel : ObservableObject
{
    /// <summary>
    /// Longer than the advice panel's 150 ms: a drag of the percentage slider fires continuously,
    /// and each request is a fresh Monte-Carlo run.
    /// </summary>
    private const int DebounceMilliseconds = 200;

    private readonly IPotEngine _potEngine;
    private readonly HeadToHeadCoordinator _coordinator;
    private readonly ILogger<HeadToHeadViewModel> _logger;

    /// <summary>True while a prefill is in flight, so its many small writes queue no calculation.</summary>
    private bool _isSeeding;

    /// <summary>
    /// True while the three card pickers are being told what the other two hold. Blocking a card
    /// can drop a pick, which republishes a selection and would otherwise re-enter this.
    /// </summary>
    private bool _isSyncingCards;

    private string? _problem;

    [ObservableProperty]
    private int _playerCount = 2;

    [ObservableProperty]
    private decimal _bigBlind = 100;

    [ObservableProperty]
    private AnteStyleChoice _anteStyle = AnteStyleChoice.All[0];

    [ObservableProperty]
    private decimal _anteAmount = 100;

    [ObservableProperty]
    private PositionChoice? _heroSeat;

    [ObservableProperty]
    private PositionChoice? _villainSeat;

    /// <summary>Twelve big blinds: the depth at which a final table stops having other options.</summary>
    [ObservableProperty]
    private decimal _heroStack = 1200;

    [ObservableProperty]
    private decimal _villainStack = 1200;

    [ObservableProperty]
    private HeadToHeadRoleChoice _role = HeadToHeadRoleChoice.All[0];

    [ObservableProperty]
    private bool _heroIsExactHand = true;

    [ObservableProperty]
    private bool _villainIsExactHand;

    [ObservableProperty]
    private bool _isEditingVillain;

    [ObservableProperty]
    private string _depthLabel = string.Empty;

    /// <summary>
    /// False until the panel is actually opened. The view models are constructed at startup and this
    /// one must stay silent until then: a Monte-Carlo run competing with the advice pipeline is the
    /// difference between compact mode answering in under a second and not.
    /// </summary>
    [ObservableProperty]
    private bool _isActive;

    public HeadToHeadViewModel(
        IPotEngine potEngine,
        HeadToHeadCoordinator coordinator,
        IPreflopHandStrength strength,
        ILogger<HeadToHeadViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(strength);

        _potEngine = potEngine;
        _coordinator = coordinator;
        _logger = logger;

        HeroRange = new RangeGridViewModel(strength);
        VillainRange = new RangeGridViewModel(strength);

        RefreshSeats();

        // A short-stack jam and the call that faces it, so the panel says something before anything
        // has been typed into it.
        HeroRange.TopPercent = 40;
        VillainRange.TopPercent = 20;

        HeroRange.Changed += OnRangeChanged;
        VillainRange.Changed += OnRangeChanged;
        Board.PropertyChanged += OnCardsChanged;
        HeroCards.PropertyChanged += OnCardsChanged;
        VillainCards.PropertyChanged += OnCardsChanged;

        RefreshDepth();
    }

    /// <summary>
    /// Asked to hand the window back to the analysis panel. An event rather than a reach into the
    /// parent, as <see cref="ChartsViewModel.Changed"/> and
    /// <see cref="JournalViewModel.ReplayRequested"/> already do.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Asked to switch language. The parent owns it: the analysis panel's computed labels have to be
    /// rebuilt too, and only it knows about those.
    /// </summary>
    public event EventHandler? LanguageToggleRequested;

    public UiText Text => UiText.Current;

    public CardPickerViewModel Board { get; } = new(5, () => UiText.Current.EmptyBoard);

    public CardPickerViewModel HeroCards { get; } = new(2, () => UiText.Current.NoHand);

    public CardPickerViewModel VillainCards { get; } = new(2, () => UiText.Current.NoHand);

    public RangeGridViewModel HeroRange { get; }

    public RangeGridViewModel VillainRange { get; }

    public HeadToHeadResultViewModel Result => _coordinator.Result;

    public ObservableCollection<PositionChoice> AvailablePositions { get; } = [];

    public IReadOnlyList<AnteStyleChoice> AnteStyles => AnteStyleChoice.All;

    public IReadOnlyList<HeadToHeadRoleChoice> Roles => HeadToHeadRoleChoice.All;

    public IReadOnlyList<int> PlayerCounts { get; } = [2, 3, 4, 5, 6, 7, 8];

    public bool IsAnteEnabled => AnteStyle.Value != Core.Table.AnteStyle.None;

    public bool IsEditingHero => !IsEditingVillain;

    public RangeGridViewModel ActiveRange => IsEditingVillain ? VillainRange : HeroRange;

    /// <summary>Whether the side being edited is entered as one hand rather than as a range.</summary>
    public bool ActiveSideIsExactHand => IsEditingVillain ? VillainIsExactHand : HeroIsExactHand;

    public CardPickerViewModel ActiveSideCards => IsEditingVillain ? VillainCards : HeroCards;

    public string ActiveRangeTitle => IsEditingVillain
        ? Role.Value == HeadToHeadRole.Jamming ? UiText.Current.HisCallingRange : UiText.Current.HisJammingRange
        : UiText.Current.MyRange;

    /// <summary>
    /// Fills the panel in from the hand being played, then computes once. Nothing here is ever
    /// written back the other way.
    /// </summary>
    public void SeedFrom(HandState state, HandAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(analysis);

        _isSeeding = true;
        try
        {
            TableConfiguration table = state.Table;

            PlayerCount = table.PlayerCount;
            BigBlind = (decimal)table.BigBlind;
            AnteStyle = AnteStyles.FirstOrDefault(choice => choice.Value == table.AnteStyle) ?? AnteStyles[0];
            AnteAmount = (decimal)table.AnteAmount;

            RefreshSeats();
            HeroSeat = Seat(table.HeroPosition);

            Position villain = analysis.LiveOpponents.Count > 0
                ? analysis.LiveOpponents[0]
                : AvailablePositions.First(choice => choice.Value != table.HeroPosition).Value;
            VillainSeat = Seat(villain);

            HeroStack = (decimal)table.StackOf(table.HeroPosition);
            VillainStack = (decimal)table.StackOf(villain);

            // Cleared first: Restore trusts its caller rather than the availability flags.
            Board.Restore([]);
            HeroCards.Restore([]);
            VillainCards.Restore([]);

            Board.Restore(state.Board);

            if (state.HeroCards is HoleCards hero)
            {
                HeroCards.Restore([hero.First, hero.Second]);
            }

            // With no hand entered yet, fall back to a range rather than opening on a complaint.
            HeroIsExactHand = state.HeroCards is not null;
            VillainIsExactHand = false;
            IsEditingVillain = true;
        }
        finally
        {
            _isSeeding = false;
        }

        IsActive = true;
        RefreshDepth();
        _ = RunAsync(0);

        _logger.LogInformation("Head-to-head seeded from the hand in progress");
    }

    /// <summary>Computes without waiting for the debounce. Used by the tests and by the prefill.</summary>
    public Task ComputeNowAsync()
    {
        IsActive = true;
        RefreshDepth();
        return RunAsync(0);
    }

    /// <summary>Rebuilds the computed labels after a language change.</summary>
    public void Refresh()
    {
        RefreshDepth();
        HeroRange.Refresh();
        VillainRange.Refresh();
        OnPropertyChanged(nameof(ActiveRangeTitle));

        Board.RefreshLabel();
        HeroCards.RefreshLabel();
        VillainCards.RefreshLabel();

        if (IsActive)
        {
            _ = RunAsync(DebounceMilliseconds);
        }
    }

    [RelayCommand]
    public void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void ToggleLanguage()
    {
        LanguageToggleRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void EditHero()
    {
        IsEditingVillain = false;
    }

    [RelayCommand]
    public void EditVillain()
    {
        IsEditingVillain = true;
    }

    [RelayCommand]
    public void SwapSides()
    {
        _isSeeding = true;
        try
        {
            (HeroStack, VillainStack) = (VillainStack, HeroStack);
            (HeroSeat, VillainSeat) = (VillainSeat, HeroSeat);
            (HeroIsExactHand, VillainIsExactHand) = (VillainIsExactHand, HeroIsExactHand);

            HandRange hero = HeroRange.Range;
            HeroRange.Set(VillainRange.Range);
            VillainRange.Set(hero);

            IReadOnlyList<Card> heroCards = [.. HeroCards.Selection];
            IReadOnlyList<Card> villainCards = [.. VillainCards.Selection];
            HeroCards.Restore([]);
            VillainCards.Restore([]);
            HeroCards.Restore(villainCards);
            VillainCards.Restore(heroCards);

            // Swapping seats swaps who is jamming at whom.
            Role = Role.Value == HeadToHeadRole.Jamming ? Roles[1] : Roles[0];
        }
        finally
        {
            _isSeeding = false;
        }

        ScheduleCompute();
    }

    partial void OnPlayerCountChanged(int value)
    {
        RefreshSeats();
        ScheduleCompute();
    }

    partial void OnBigBlindChanged(decimal value)
    {
        ScheduleCompute();
    }

    partial void OnAnteStyleChanged(AnteStyleChoice value)
    {
        OnPropertyChanged(nameof(IsAnteEnabled));
        ScheduleCompute();
    }

    partial void OnAnteAmountChanged(decimal value)
    {
        ScheduleCompute();
    }

    partial void OnHeroSeatChanged(PositionChoice? value)
    {
        ScheduleCompute();
    }

    partial void OnVillainSeatChanged(PositionChoice? value)
    {
        ScheduleCompute();
    }

    partial void OnHeroStackChanged(decimal value)
    {
        ScheduleCompute();
    }

    partial void OnVillainStackChanged(decimal value)
    {
        ScheduleCompute();
    }

    partial void OnRoleChanged(HeadToHeadRoleChoice value)
    {
        OnPropertyChanged(nameof(ActiveRangeTitle));
        ScheduleCompute();
    }

    partial void OnHeroIsExactHandChanged(bool value)
    {
        OnPropertyChanged(nameof(ActiveSideIsExactHand));
        ScheduleCompute();
    }

    partial void OnVillainIsExactHandChanged(bool value)
    {
        OnPropertyChanged(nameof(ActiveSideIsExactHand));
        ScheduleCompute();
    }

    partial void OnIsEditingVillainChanged(bool value)
    {
        OnPropertyChanged(nameof(IsEditingHero));
        OnPropertyChanged(nameof(ActiveRange));
        OnPropertyChanged(nameof(ActiveRangeTitle));
        OnPropertyChanged(nameof(ActiveSideIsExactHand));
        OnPropertyChanged(nameof(ActiveSideCards));
    }

    private void OnRangeChanged(object? sender, EventArgs args)
    {
        ScheduleCompute();
    }

    private void OnCardsChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(CardPickerViewModel.Selection) || _isSyncingCards)
        {
            return;
        }

        _isSyncingCards = true;
        try
        {
            Board.SetUnavailable([.. HeroCards.Selection, .. VillainCards.Selection]);
            HeroCards.SetUnavailable([.. Board.Selection, .. VillainCards.Selection]);
            VillainCards.SetUnavailable([.. Board.Selection, .. HeroCards.Selection]);
        }
        finally
        {
            _isSyncingCards = false;
        }

        ScheduleCompute();
    }

    private void ScheduleCompute()
    {
        if (_isSeeding)
        {
            return;
        }

        RefreshDepth();

        if (!IsActive)
        {
            return;
        }

        _ = RunAsync(DebounceMilliseconds);
    }

    private Task RunAsync(int delayMilliseconds)
    {
        HeadToHeadRequest? request;
        try
        {
            request = BuildRequest();
        }
        catch (PokerRangesException exception)
        {
            // An inconsistent table or an impossible seat: say so rather than throwing at the user.
            _coordinator.ShowProblem(exception.Message);
            return Task.CompletedTask;
        }

        if (request is null)
        {
            _coordinator.ShowProblem(_problem ?? UiText.Current.PickBothRanges);
            return Task.CompletedTask;
        }

        return _coordinator.ShowAsync(request, delayMilliseconds);
    }

    private HeadToHeadRequest? BuildRequest()
    {
        _problem = null;

        // The equity engine only accepts a board of nothing, a flop, a turn or a river.
        if (Board.Selection.Count is 1 or 2)
        {
            _problem = UiText.Current.EmptyBoard;
            return null;
        }

        HoleCards? heroCards = HeroIsExactHand ? HeroCards.AsHoleCards : null;
        HoleCards? villainCards = VillainIsExactHand ? VillainCards.AsHoleCards : null;

        HandRange? heroRange = SideRange(HeroIsExactHand, heroCards, HeroRange.Range);
        HandRange? villainRange = SideRange(VillainIsExactHand, villainCards, VillainRange.Range);

        if (heroRange is null || villainRange is null)
        {
            _problem = UiText.Current.PickBothRanges;
            return null;
        }

        if (HeroSeat is not PositionChoice hero || VillainSeat is not PositionChoice villain)
        {
            _problem = UiText.Current.PickBothRanges;
            return null;
        }

        TableConfiguration table = BuildTable(hero.Value, villain.Value);
        HeadToHeadSpot spot = HeadToHeadSpot.BetweenSeats(_potEngine, table, villain.Value, Role.Value);

        return new HeadToHeadRequest
        {
            HeroRange = heroRange,
            VillainRange = villainRange,
            Spot = spot,
            Board = [.. Board.Selection],
            HeroCards = heroCards,
            VillainCards = villainCards,
        };
    }

    private TableConfiguration BuildTable(Position hero, Position villain)
    {
        Dictionary<Position, double> stacks = [];
        foreach (Position seat in PositionLayout.Seats(PlayerCount))
        {
            // The folded seats only ever contribute their blinds and antes, so their stack merely has
            // to cover them.
            stacks[seat] = seat == hero
                ? (double)HeroStack
                : seat == villain
                    ? (double)VillainStack
                    : (double)Math.Max(HeroStack, VillainStack);
        }

        return new TableConfiguration
        {
            PlayerCount = PlayerCount,
            BigBlind = (double)BigBlind,
            StartingStacks = stacks,
            HeroPosition = hero,
            AnteStyle = AnteStyle.Value,
            AnteAmount = IsAnteEnabled ? (double)AnteAmount : 0,
        };
    }

    private static HandRange? SideRange(bool isExactHand, HoleCards? cards, HandRange range)
    {
        if (isExactHand)
        {
            return cards is HoleCards combo
                ? new HandRangeBuilder().Set(combo, 1).Build()
                : null;
        }

        return range.IsEmpty ? null : range;
    }

    private PositionChoice? Seat(Position position)
    {
        return AvailablePositions.FirstOrDefault(choice => choice.Value == position);
    }

    private void RefreshSeats()
    {
        Position previousHero = HeroSeat?.Value ?? Position.Button;
        Position previousVillain = VillainSeat?.Value ?? Position.BigBlind;

        AvailablePositions.Clear();
        foreach (Position seat in PositionLayout.PreflopOrder(PlayerCount))
        {
            AvailablePositions.Add(PositionChoice.Of(seat));
        }

        HeroSeat = Seat(previousHero) ?? AvailablePositions[^2];
        VillainSeat = Seat(previousVillain) ?? AvailablePositions[^1];

        if (VillainSeat?.Value == HeroSeat?.Value)
        {
            VillainSeat = AvailablePositions.First(choice => choice.Value != HeroSeat!.Value);
        }
    }

    private void RefreshDepth()
    {
        decimal effective = Math.Min(HeroStack, VillainStack);

        DepthLabel = BigBlind <= 0
            ? UiMatrixText.DepthUnknown
            : UiHeadToHeadText.DepthLabel((double)(effective / BigBlind));
    }
}
