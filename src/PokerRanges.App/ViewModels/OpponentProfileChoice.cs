using CommunityToolkit.Mvvm.ComponentModel;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Postflop;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// Une entrée de la liste des profils adverses. Comme pour les antes, l'identité de l'entrée porte
/// la sélection et son libellé suit la langue.
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
