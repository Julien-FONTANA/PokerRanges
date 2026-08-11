using CommunityToolkit.Mvvm.ComponentModel;
using PokerRanges.App.Localization;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Table;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// Une entrée de la liste des antes. Les instances sont uniques et durables — c'est leur identité
/// qui porte la sélection — donc le libellé ne peut pas être figé à la construction : il est relu
/// à chaque affichage, et l'entrée se signale elle-même quand la langue change.
/// </summary>
public sealed class AnteStyleChoice : ObservableObject
{
    private readonly Func<string> _text;

    private AnteStyleChoice(AnteStyle value, Func<string> text)
    {
        Value = value;
        _text = text;

        Language.Changed += (_, _) => OnPropertyChanged(nameof(Label));
    }

    public AnteStyle Value { get; }

    public string Label => _text();

    public static IReadOnlyList<AnteStyleChoice> All { get; } =
    [
        new(AnteStyle.None, () => UiText.Current.AnteNone),
        new(AnteStyle.BigBlindAnte, () => UiText.Current.AnteBigBlind),
        new(AnteStyle.PerPlayer, () => UiText.Current.AntePerPlayer),
    ];
}
