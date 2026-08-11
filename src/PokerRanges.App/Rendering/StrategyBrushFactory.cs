using Avalonia;
using Avalonia.Media;
using PokerRanges.Core.Preflop;

namespace PokerRanges.App.Rendering;

/// <summary>
/// Peint une case de la grille en bandes horizontales proportionnelles aux fréquences du chart.
/// Un dégradé à paliers francs donne exactement ce découpage avec une seule brosse, sans avoir à
/// empiler des rectangles dans la mise en page.
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
