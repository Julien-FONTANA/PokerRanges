using PokerRanges.Core.Cards;

namespace PokerRanges.Core.Postflop;

public sealed record RankedCombo(HoleCards Combo, double Weight, double Equity);
