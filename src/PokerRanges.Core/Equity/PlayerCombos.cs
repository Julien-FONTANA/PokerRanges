using PokerRanges.Core.Cards;
using PokerRanges.Core.Ranges;

namespace PokerRanges.Core.Equity;

/// <summary>
/// A player's range flattened for computation: the combos still possible given the board and the
/// dead cards, with cumulative weights for weighted sampling in logarithmic time.
/// </summary>
internal sealed class PlayerCombos
{
    private readonly double[] _cumulativeWeights;

    private PlayerCombos(HoleCards[] combos, double[] weights, double[] cumulativeWeights, double totalWeight)
    {
        Combos = combos;
        Weights = weights;
        _cumulativeWeights = cumulativeWeights;
        TotalWeight = totalWeight;
    }

    public HoleCards[] Combos { get; }

    public double[] Weights { get; }

    public double TotalWeight { get; }

    public int Length => Combos.Length;

    public static PlayerCombos Create(HandRange range, ReadOnlySpan<bool> blockedCards)
    {
        List<HoleCards> combos = [];
        List<double> weights = [];

        foreach (WeightedCombo entry in range.EnumerateCombos())
        {
            if (blockedCards[entry.Combo.First.Index] || blockedCards[entry.Combo.Second.Index])
            {
                continue;
            }

            combos.Add(entry.Combo);
            weights.Add(entry.Weight);
        }

        double[] cumulative = new double[weights.Count];
        double running = 0;
        for (int index = 0; index < weights.Count; index++)
        {
            running += weights[index];
            cumulative[index] = running;
        }

        return new PlayerCombos([.. combos], [.. weights], cumulative, running);
    }

    public HoleCards Sample(double uniform)
    {
        double target = uniform * TotalWeight;

        int low = 0;
        int high = _cumulativeWeights.Length - 1;
        while (low < high)
        {
            int middle = (low + high) / 2;
            if (_cumulativeWeights[middle] < target)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return Combos[low];
    }
}
