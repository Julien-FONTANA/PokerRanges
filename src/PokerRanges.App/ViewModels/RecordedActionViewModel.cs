using Avalonia.Media;
using PokerRanges.App.Localization;
using PokerRanges.Core.Table;

namespace PokerRanges.App.ViewModels;

public sealed record RecordedActionViewModel(PlayerAction Action, string Label, bool IsHero)
{
    private static readonly SolidColorBrush HeroBrush = new(Color.Parse("#F2C94C"));
    private static readonly SolidColorBrush OpponentBrush = new(Color.Parse("#C8C8C8"));

    public IBrush Foreground => IsHero ? HeroBrush : OpponentBrush;

    public static RecordedActionViewModel From(PlayerAction action, Position heroPosition)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new RecordedActionViewModel(
            action,
            UiMatrixText.RecordedAction(PositionLayout.Describe(action.Position), action.Kind, action.AmountTo),
            action.Position == heroPosition);
    }
}
