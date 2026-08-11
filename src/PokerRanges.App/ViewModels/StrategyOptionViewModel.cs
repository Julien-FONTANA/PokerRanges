using System.Globalization;
using Avalonia.Media;
using PokerRanges.App.Rendering;
using PokerRanges.Core.Preflop;

namespace PokerRanges.App.ViewModels;

public sealed record StrategyOptionViewModel(string Label, string Percent, double BarWidth, IBrush Brush)
{
    private const double MaximumBarWidth = 220;

    public static StrategyOptionViewModel From(StrategyOption option)
    {
        ArgumentNullException.ThrowIfNull(option);

        return new StrategyOptionViewModel(
            option.Describe(),
            option.Frequency.ToString("P0", CultureInfo.CurrentCulture),
            Math.Max(2, option.Frequency * MaximumBarWidth),
            ActionPalette.BrushOf(option.Kind));
    }
}
