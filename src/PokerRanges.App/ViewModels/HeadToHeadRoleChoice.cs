using CommunityToolkit.Mvvm.ComponentModel;
using PokerRanges.App.Localization;
using PokerRanges.Core.HeadToHead;
using PokerRanges.Core.Localization;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// One entry in the situation list. Long-lived and unique, like <see cref="AnteStyleChoice"/>: its
/// identity carries the selection, so the label is re-read rather than frozen at construction.
/// </summary>
public sealed class HeadToHeadRoleChoice : ObservableObject
{
    private readonly Func<string> _text;

    private HeadToHeadRoleChoice(HeadToHeadRole value, Func<string> text)
    {
        Value = value;
        _text = text;

        Language.Changed += (_, _) => OnPropertyChanged(nameof(Label));
    }

    public HeadToHeadRole Value { get; }

    public string Label => _text();

    public static IReadOnlyList<HeadToHeadRoleChoice> All { get; } =
    [
        new(HeadToHeadRole.Jamming, () => UiText.Current.RoleJamming),
        new(HeadToHeadRole.CallingAJam, () => UiText.Current.RoleCallingAJam),
    ];
}
