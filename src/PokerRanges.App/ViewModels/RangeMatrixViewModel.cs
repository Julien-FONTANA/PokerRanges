using System.Globalization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using PokerRanges.App.Localization;
using PokerRanges.App.Rendering;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Preflop;

namespace PokerRanges.App.ViewModels;

public sealed partial class RangeMatrixViewModel : ObservableObject
{
    [ObservableProperty]
    private IReadOnlyList<RangeMatrixCellViewModel> _cells = BuildEmptyCells();

    [ObservableProperty]
    private string _title = UiMatrixText.GridPlaceholderTitle;

    [ObservableProperty]
    private string _subtitle = UiMatrixText.GridPlaceholderSubtitle;

    /// <summary>
    /// The legend describes what the grid is showing right now, and so changes with it: actions
    /// preflop, quantities postflop. A fixed legend would lie half the time.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<LegendEntryViewModel> _legend = LegendEntryViewModel.Actions;

    /// <summary>Why the hero's cell is dark, when it is.</summary>
    [ObservableProperty]
    private string? _heroNote;

    public UiText Text => UiText.Current;

    public bool HasHeroNote => HeroNote is not null;

    public void Show(RangeStrategy strategy, HandClass? heroHand, string title, string subtitle)
    {
        ArgumentNullException.ThrowIfNull(strategy);

        List<RangeMatrixCellViewModel> cells = new(HandClass.Count);

        foreach (HandClass handClass in HandClass.All)
        {
            IReadOnlyList<StrategyOption> options = strategy.OptionsFor(handClass);

            cells.Add(new RangeMatrixCellViewModel(
                handClass.ToString(),
                StrategyBrushFactory.Build(options),
                handClass == heroHand,
                BuildTooltip(handClass, options)));
        }

        Cells = cells;
        Title = title;
        Subtitle = subtitle;
        Legend = LegendEntryViewModel.Actions;
        HeroNote = null;
    }

    /// <summary>
    /// Displays a plain range — the one assigned to an opponent — as a green intensity
    /// proportional to the share of the cell still present in their range.
    /// </summary>
    public void ShowRange(
        Core.Ranges.HandRange range,
        HandClass? heroHand,
        IReadOnlyList<Card> deadCards,
        string opponent,
        string title,
        string subtitle)
    {
        ArgumentNullException.ThrowIfNull(range);
        ArgumentNullException.ThrowIfNull(deadCards);

        List<RangeMatrixCellViewModel> cells = new(HandClass.Count);

        foreach (HandClass handClass in HandClass.All)
        {
            double frequency = range.FrequencyOf(handClass);

            cells.Add(new RangeMatrixCellViewModel(
                handClass.ToString(),
                ActionPalette.RangeWeightBrush(frequency),
                handClass == heroHand,
                UiMatrixText.CellShare(handClass, frequency)));
        }

        Cells = cells;
        Title = title;
        Subtitle = subtitle;
        Legend = LegendEntryViewModel.RangeWeights;
        HeroNote = DescribeHeroCell(range, heroHand, deadCards, opponent);
    }

    public void ShowNothing(string subtitle)
    {
        Cells = BuildEmptyCells();
        Title = UiMatrixText.GridPlaceholderTitle;
        Subtitle = subtitle;
        Legend = LegendEntryViewModel.Actions;
        HeroNote = null;
    }

    partial void OnHeroNoteChanged(string? value)
    {
        OnPropertyChanged(nameof(HasHeroNote));
    }

    /// <summary>
    /// A dark hero cell has two very different causes, and conflating them would be worse than
    /// saying nothing: either the opponent can no longer hold that hand — your cards and the board
    /// take up all its combos — or they could have it but their range does not contain it.
    /// </summary>
    private static string? DescribeHeroCell(
        Core.Ranges.HandRange range,
        HandClass? heroHand,
        IReadOnlyList<Card> deadCards,
        string opponent)
    {
        if (heroHand is not HandClass hand || range.WeightOf(hand) > 0.001)
        {
            return null;
        }

        double stillPossible = Core.Ranges.HandRange.Full.WithoutCards([.. deadCards]).WeightOf(hand);

        return stillPossible <= 0.001
            ? UiMatrixText.HeroHandBlocked(hand, hand.CombinationCount)
            : UiMatrixText.HeroHandOutsideRange(hand, opponent);
    }

    private static string BuildTooltip(HandClass handClass, IReadOnlyList<StrategyOption> options)
    {
        IEnumerable<string> parts = options
            .Where(option => option.Frequency > 0.001)
            .OrderBy(option => ActionPalette.SortOrderOf(option.Kind))
            .Select(option => UiMatrixText.CellOption(
                option.Describe(),
                option.Frequency.ToString("P0", CultureInfo.CurrentCulture)));

        return $"{handClass}\n{string.Join("\n", parts)}";
    }

    private static IReadOnlyList<RangeMatrixCellViewModel> BuildEmptyCells()
    {
        List<RangeMatrixCellViewModel> cells = new(HandClass.Count);

        foreach (HandClass handClass in HandClass.All)
        {
            cells.Add(new RangeMatrixCellViewModel(
                handClass.ToString(),
                ActionPalette.BrushOf(ChartActionKind.Fold),
                false,
                handClass.ToString()));
        }

        return cells;
    }
}
