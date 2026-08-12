using PokerRanges.Core.Cards;

namespace PokerRanges.Core.Ranges;

/// <summary>One of the 169 starting hands and how it fares against a hand drawn at random.</summary>
public sealed record RankedHandClass(HandClass HandClass, double EquityAgainstRandomHand);
