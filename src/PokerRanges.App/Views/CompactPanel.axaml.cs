using Avalonia.Controls;
using Avalonia.Input;

namespace PokerRanges.App.Views;

/// <summary>
/// The reduced layout: the entry fields and the recommendation, nothing else. Enter moves from one
/// field to the next so a whole hand can be typed without leaving the keyboard.
/// </summary>
public sealed partial class CompactPanel : UserControl
{
    public CompactPanel()
    {
        InitializeComponent();

        HeroEntry.KeyDown += OnEntryKeyDown;
        BoardEntry.KeyDown += OnEntryKeyDown;
    }

    /// <summary>Hands focus back to the entry field, called when compact mode opens.</summary>
    public void FocusEntry()
    {
        GiveFocusTo(HeroEntry);
    }

    private void OnEntryKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key != Key.Enter)
        {
            return;
        }

        GiveFocusTo(ReferenceEquals(sender, HeroEntry) ? BoardEntry : HeroEntry);
        args.Handled = true;
    }

    /// <summary>
    /// Caret at the end of the text rather than a full selection: switching to compact mid-hand
    /// must not leave the hand already typed one keystroke away from being wiped.
    /// </summary>
    private static void GiveFocusTo(TextBox entry)
    {
        entry.Focus();
        entry.CaretIndex = entry.Text?.Length ?? 0;
    }
}
