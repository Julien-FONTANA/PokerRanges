using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PokerRanges.App.Localization;
using PokerRanges.Core.Session;
using PokerRanges.Core.Table;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// The table settings. Amounts are <see cref="decimal"/> because that is the type Avalonia's
/// numeric fields expect; they are converted to <see cref="double"/> at the engine's boundary.
/// </summary>
public sealed partial class TableSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _playerCount = 8;

    [ObservableProperty]
    private decimal _bigBlind = 8;

    [ObservableProperty]
    private decimal _startingStack = 1000;

    [ObservableProperty]
    private AnteStyleChoice _anteStyle = AnteStyleChoice.All[0];

    [ObservableProperty]
    private decimal _anteAmount = 8;

    [ObservableProperty]
    private PositionChoice? _heroPosition;

    [ObservableProperty]
    private string _depthLabel = string.Empty;

    public TableSettingsViewModel()
    {
        RefreshPositions();
        RefreshDepth();
    }

    public UiText Text => UiText.Current;

    public IReadOnlyList<int> PlayerCounts { get; } = [2, 3, 4, 5, 6, 7, 8];

    public IReadOnlyList<AnteStyleChoice> AnteStyles => AnteStyleChoice.All;

    public ObservableCollection<PositionChoice> AvailablePositions { get; } = [];

    public bool IsAnteEnabled => AnteStyle.Value != Core.Table.AnteStyle.None;

    /// <summary>
    /// Reloads the settings. Order matters: changing the player count redistributes the available
    /// seats, so the hero's position can only be set afterwards.
    /// </summary>
    public void Apply(UserPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        PlayerCount = preferences.PlayerCount;
        BigBlind = (decimal)preferences.BigBlind;
        StartingStack = (decimal)preferences.StartingStack;
        AnteStyle = AnteStyles.FirstOrDefault(choice => choice.Value == preferences.AnteStyle) ?? AnteStyles[0];
        AnteAmount = (decimal)preferences.AnteAmount;
        HeroPosition = AvailablePositions.FirstOrDefault(choice => choice.Value == preferences.HeroPosition)
                       ?? AvailablePositions[^1];
    }

    /// <summary>The table settings as displayed; the rest is filled in by the caller.</summary>
    public UserPreferences Capture()
    {
        return new UserPreferences
        {
            PlayerCount = PlayerCount,
            BigBlind = (double)BigBlind,
            StartingStack = (double)StartingStack,
            AnteStyle = AnteStyle.Value,
            AnteAmount = (double)AnteAmount,
            HeroPosition = HeroPosition?.Value ?? Position.Button,
        };
    }

    /// <summary>
    /// Rebuilds the only computed label here. Above all, it notifies no other property: the main
    /// window reads a settings change as the start of a new hand, and translating the screen would
    /// wipe the hand in progress.
    /// </summary>
    public void Refresh()
    {
        RefreshDepth();
    }

    public TableConfiguration Build()
    {
        Position hero = HeroPosition?.Value ?? Position.BigBlind;

        return TableConfiguration.Uniform(
            PlayerCount,
            (double)BigBlind,
            (double)StartingStack,
            hero) with
        {
            AnteStyle = AnteStyle.Value,
            AnteAmount = IsAnteEnabled ? (double)AnteAmount : 0,
        };
    }

    partial void OnPlayerCountChanged(int value)
    {
        RefreshPositions();
    }

    partial void OnBigBlindChanged(decimal value)
    {
        RefreshDepth();
    }

    partial void OnStartingStackChanged(decimal value)
    {
        RefreshDepth();
    }

    partial void OnAnteStyleChanged(AnteStyleChoice value)
    {
        OnPropertyChanged(nameof(IsAnteEnabled));
    }

    private void RefreshDepth()
    {
        DepthLabel = BigBlind <= 0
            ? UiMatrixText.DepthUnknown
            : UiMatrixText.DepthLabel(StartingStack / BigBlind);
    }

    private void RefreshPositions()
    {
        Position previous = HeroPosition?.Value ?? Position.Button;

        AvailablePositions.Clear();
        foreach (Position seat in PositionLayout.PreflopOrder(PlayerCount))
        {
            AvailablePositions.Add(PositionChoice.Of(seat));
        }

        HeroPosition = AvailablePositions.FirstOrDefault(choice => choice.Value == previous)
                       ?? AvailablePositions[^1];
    }
}
