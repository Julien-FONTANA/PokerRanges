using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PokerRanges.App.Localization;
using PokerRanges.Core.Session;
using PokerRanges.Core.Table;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// Les réglages de la table. Les montants sont en <see cref="decimal"/> parce que c'est le type
/// qu'attendent les champs numériques d'Avalonia ; ils sont convertis en <see cref="double"/> à la
/// frontière du moteur.
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
    /// Recharge les réglages. L'ordre compte : changer le nombre de joueurs redistribue les sièges
    /// disponibles, donc la position du héros ne peut être posée qu'après.
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

    /// <summary>Les réglages de table tels qu'ils sont affichés ; le reste est rempli par l'appelant.</summary>
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
    /// Reconstruit le seul libellé calculé ici. Surtout, ne signale aucune autre propriété : la
    /// fenêtre principale lit un changement de réglage comme le début d'une nouvelle main, et
    /// traduire l'écran effacerait la main en cours.
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
