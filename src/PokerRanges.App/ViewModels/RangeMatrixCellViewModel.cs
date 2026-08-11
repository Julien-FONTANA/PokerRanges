using Avalonia;
using Avalonia.Media;

namespace PokerRanges.App.ViewModels;

public sealed record RangeMatrixCellViewModel(
    string Label,
    IBrush Background,
    bool IsHeroHand,
    string Tooltip)
{
    private static readonly SolidColorBrush HeroBorder = new(Color.Parse("#F2C94C"));
    private static readonly SolidColorBrush PlainBorder = new(Color.Parse("#141414"));

    public IBrush BorderBrush => IsHeroHand ? HeroBorder : PlainBorder;

    public Thickness BorderThickness => IsHeroHand ? new Thickness(2) : new Thickness(1);
}
