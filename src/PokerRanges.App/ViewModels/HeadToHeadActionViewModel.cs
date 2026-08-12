using Avalonia.Media;
using PokerRanges.App.Localization;
using PokerRanges.App.Rendering;
using PokerRanges.Core.HeadToHead;

namespace PokerRanges.App.ViewModels;

public sealed record HeadToHeadActionViewModel(string Label, string Detail, string Value, IBrush Accent)
{
    public static IReadOnlyList<HeadToHeadActionViewModel> From(HeadToHeadResult result, double bigBlind)
    {
        ArgumentNullException.ThrowIfNull(result);

        return
        [
            .. result.Actions.Select(action => new HeadToHeadActionViewModel(
                action.Label,
                action.Explanation,
                UiHeadToHeadText.ExpectedValue(action.ExpectedValueInBigBlinds(bigBlind)),
                ActionPalette.BrushOf(action.Kind))),
        ];
    }
}
