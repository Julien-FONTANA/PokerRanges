using PokerRanges.Core.Cards;

namespace PokerRanges.Core.Ranges;

public readonly record struct WeightedCombo(HoleCards Combo, double Weight);
