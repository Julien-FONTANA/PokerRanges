using Avalonia.Controls;
using Avalonia.Input;

namespace PokerRanges.App.Views;

/// <summary>
/// La disposition réduite : la saisie et la recommandation, rien d'autre. Entrée fait passer d'un
/// champ à l'autre pour qu'une main entière se tape sans lâcher le clavier.
/// </summary>
public sealed partial class CompactPanel : UserControl
{
    public CompactPanel()
    {
        InitializeComponent();

        HeroEntry.KeyDown += OnEntryKeyDown;
        BoardEntry.KeyDown += OnEntryKeyDown;
    }

    /// <summary>Rend la main au champ de saisie, appelé à l'entrée en mode compact.</summary>
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
    /// Curseur en fin de texte plutôt que sélection complète : basculer en compact au milieu d'une
    /// main ne doit pas mettre la main déjà saisie à un caractère de l'effacement.
    /// </summary>
    private static void GiveFocusTo(TextBox entry)
    {
        entry.Focus();
        entry.CaretIndex = entry.Text?.Length ?? 0;
    }
}
