using PokerRanges.Core.Ranges;

namespace PokerRanges.Core.Preflop;

internal sealed record StrategyBranch(ChartActionKind Kind, double SizeInBigBlinds, HandRange Range);
