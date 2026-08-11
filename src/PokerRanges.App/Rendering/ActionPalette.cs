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
    /// La teinte d'une case de range attribuée : l'intensité dit quelle part de la case reste
    /// possible chez l'adversaire. Une case vide reprend le gris du fold — d'où l'importance que la
    /// légende dise « absente de sa range » et non « passe », sans quoi le lecteur croit lire une
    /// action alors qu'il lit une absence.
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

    /// <summary>Ordre d'affichage : les actions les plus agressives à gauche, le fold à droite.</summary>
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
