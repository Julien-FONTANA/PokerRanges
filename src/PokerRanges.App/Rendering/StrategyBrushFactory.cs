using Avalonia;
using Avalonia.Media;
using PokerRanges.Core.Preflop;

namespace PokerRanges.App.Rendering;

/// <summary>
/// Paints a grid cell as horizontal bands proportional to the chart's frequencies. A gradient with
/// hard stops gives exactly that split with a single brush, without having to stack rectangles in
/// the layout.
/// </summary>
public static class StrategyBrushFactory
{
    public static IBrush Build(IReadOnlyList<StrategyOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<StrategyOption> ordered =
        [
            .. options
                .Where(option => option.Frequency > 0.001)
                .OrderBy(option => ActionPalette.SortOrderOf(option.Kind)),
        ];

        if (ordered.Count == 0)
        {
            return ActionPalette.BrushOf(ChartActionKind.Fold);
        }

        if (ordered.Count == 1)
        {
            return ActionPalette.BrushOf(ordered[0].Kind);
        }

        double total = ordered.Sum(option => option.Frequency);
        GradientStops stops = [];
        double cursor = 0;

        foreach (StrategyOption option in ordered)
        {
            Color colour = ActionPalette.ColourOf(option.Kind);
            double next = cursor + (option.Frequency / total);

            stops.Add(new GradientStop(colour, cursor));
            stops.Add(new GradientStop(colour, next));

            cursor = next;
        }

        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops = stops,
        };
    }
}
