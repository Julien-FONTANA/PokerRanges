using PokerRanges.Core.Ranges;
using PokerRanges.Core.Table;

namespace PokerRanges.Core.Postflop;

/// <summary>
/// La range attribuée à un adversaire, avec le récit qui l'a produite : de quel chart préflop elle
/// part et comment chacune de ses actions l'a resserrée. Sans ce récit, une range assignée n'est
/// qu'une affirmation.
/// </summary>
public sealed record OpponentRange(Position Position, HandRange Range, IReadOnlyList<string> Story)
{
    public double Combos => Range.TotalCombos;
}
