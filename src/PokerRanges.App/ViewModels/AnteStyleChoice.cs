using CommunityToolkit.Mvvm.ComponentModel;
using PokerRanges.App.Localization;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Table;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// One entry in the ante list. The instances are unique and long-lived — their identity is what
/// carries the selection — so the label cannot be frozen at construction: it is re-read on every
/// display, and the entry announces itself when the language changes.
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
