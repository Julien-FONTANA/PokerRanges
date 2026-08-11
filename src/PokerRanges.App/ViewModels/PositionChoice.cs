using PokerRanges.Core.Table;

namespace PokerRanges.App.ViewModels;

public sealed record PositionChoice(Position Value, string Label)
{
    public static PositionChoice Of(Position position)
    {
        return new PositionChoice(position, PositionLayout.Describe(position));
    }
}
