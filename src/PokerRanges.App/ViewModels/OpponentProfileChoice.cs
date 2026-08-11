using CommunityToolkit.Mvvm.ComponentModel;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Postflop;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// One entry in the opponent profile list. As with the antes, the entry's identity carries the
/// selection and its label follows the language.
/// </summary>
public sealed class OpponentProfileChoice : ObservableObject
{
    private OpponentProfileChoice(OpponentProfile value)
    {
        Value = value;

        Language.Changed += (_, _) => OnPropertyChanged(nameof(Label));
    }

    public OpponentProfile Value { get; }

    public string Label => Value.Name;

    public static IReadOnlyList<OpponentProfileChoice> All { get; } =
        [.. OpponentProfile.All.Select(profile => new OpponentProfileChoice(profile))];

    public static OpponentProfileChoice Of(OpponentProfile? profile)
    {
        return All.FirstOrDefault(choice => choice.Value == profile) ?? All[0];
    }
}
