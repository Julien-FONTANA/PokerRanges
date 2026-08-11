using Avalonia.Media;
using PokerRanges.Core.Postflop;
using PokerRanges.Core.Preflop;

namespace PokerRanges.App.Rendering;

public static class ActionPalette
{
    public static Color RangeColour { get; } = Color.Parse("#27AE60");

    public static IBrush BrushOf(PostflopActionKind kind)
    {
        return new SolidColorBrush(kind switch
        {
            PostflopActionKind.Bet => Color.Parse("#C0392B"),
            PostflopActionKind.Raise => Color.Parse("#E67E22"),
            PostflopActionKind.Call => Color.Parse("#27AE60"),
            PostflopActionKind.Check => Color.Parse("#3A6EA5"),
            _ => Color.Parse("#5A5A5A"),
        });
    }

    public static Color ColourOf(ChartActionKind kind)
    {
        return kind switch
        {
            ChartActionKind.Jam => Color.Parse("#E67E22"),
            ChartActionKind.Raise => Color.Parse("#C0392B"),
            ChartActionKind.Call => Color.Parse("#27AE60"),
            _ => Color.Parse("#2F3136"),
        };
    }

    public static IBrush BrushOf(ChartActionKind kind)
    {
        return new SolidColorBrush(ColourOf(kind));
    }

    /// <summary>
    /// The shade of an assigned-range cell: the intensity says what share of the cell is still
    /// possible for the opponent. An empty cell reuses the fold grey — hence the importance of the
    /// legend saying "not in their range" and not "folds", without which the reader thinks they
    /// are reading an action when they are reading an absence.
    /// </summary>
    public static IBrush RangeWeightBrush(double frequency)
    {
        if (frequency <= 0.001)
        {
            return BrushOf(ChartActionKind.Fold);
        }

        byte alpha = (byte)Math.Clamp(60 + (frequency * 195), 0, 255);

        return new SolidColorBrush(Color.FromArgb(alpha, RangeColour.R, RangeColour.G, RangeColour.B));
    }

    /// <summary>Display order: the most aggressive actions on the left, folding on the right.</summary>
    public static int SortOrderOf(ChartActionKind kind)
    {
        return kind switch
        {
            ChartActionKind.Jam => 0,
            ChartActionKind.Raise => 1,
            ChartActionKind.Call => 2,
            _ => 3,
        };
    }
}
