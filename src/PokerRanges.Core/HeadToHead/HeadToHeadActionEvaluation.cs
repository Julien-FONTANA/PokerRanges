using PokerRanges.Core.Preflop;

namespace PokerRanges.Core.HeadToHead;

/// <summary>
/// One option and what it is worth, in chips counted from now on: folding is worth zero, and
/// whatever is already in the pot is gone either way.
/// </summary>
public sealed record HeadToHeadActionEvaluation
{
    /// <summary>
    /// <see cref="ChartActionKind"/> rather than a new enum: it is the only action kind in the
    /// codebase that carries <see cref="ChartActionKind.Jam"/>, and the palette already paints it.
    /// </summary>
    public required ChartActionKind Kind { get; init; }

    /// <summary>Chips this action puts at risk.</summary>
    public required double Amount { get; init; }

    public required double ExpectedValue { get; init; }

    public required string Label { get; init; }

    public required string Explanation { get; init; }

    public double ExpectedValueInBigBlinds(double bigBlind)
    {
        return bigBlind <= 0 ? 0 : ExpectedValue / bigBlind;
    }
}
