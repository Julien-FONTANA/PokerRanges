using Avalonia.Controls;

namespace PokerRanges.App.Views;

/// <summary>
/// The 52 cards, four rows of thirteen. Kept apart from <see cref="CardPickerPanel"/> because
/// compact mode shows the grid alone, pointed at whichever picker is being filled.
/// </summary>
public sealed partial class CardGridView : UserControl
{
    public CardGridView()
    {
        InitializeComponent();
    }
}
