using Avalonia.Controls;

namespace PokerRanges.App.Views;

/// <summary>
/// The reduced layout: the card grid, the actions and the recommendation, nothing else. Entering a
/// hand is a run of clicks — no typing, no aiming at a field — because at the table the mouse is
/// already in hand and the keyboard is not.
/// </summary>
public sealed partial class CompactPanel : UserControl
{
    public CompactPanel()
    {
        InitializeComponent();
    }
}
