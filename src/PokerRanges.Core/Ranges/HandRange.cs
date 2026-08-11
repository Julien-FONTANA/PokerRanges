using PokerRanges.Core.Cards;

namespace PokerRanges.Core.Ranges;

/// <summary>
/// An immutable range: a weight from 0 to 1 for each of the 1326 combos. Weighting per combo and
/// not per grid cell is what makes it possible to cleanly remove the combos blocked by the board
/// and by the hero's cards.
/// </summary>
public sealed class HandRange
{
    private readonly double[] _weights;

    internal HandRange(double[] weights)
    {
        _weights = weights;

        double total = 0;
        foreach (double weight in weights)
        {
            total += weight;
        }

        TotalCombos = total;
    }

    public static HandRange Empty { get; } = new(new double[HoleCards.Count]);

    public static HandRange Full { get; } = CreateFull();

    public double TotalCombos { get; }

    public bool IsEmpty => TotalCombos <= 0;

    public double PercentOfAllHands => TotalCombos * 100.0 / HoleCards.Count;

    public double GetWeight(HoleCards combo)
    {
        return _weights[combo.Index];
    }

    public double WeightOf(HandClass handClass)
    {
        double total = 0;
        foreach (HoleCards combo in handClass.Combos())
        {
            total += _weights[combo.Index];
        }

        return total;
    }

    /// <summary>
    /// The share of the cell actually contained in the range, from 0 to 1: this is the fill the
    /// 13x13 grid displays.
    /// </summary>
    public double FrequencyOf(HandClass handClass)
    {
        return WeightOf(handClass) / handClass.CombinationCount;
    }

    public IEnumerable<WeightedCombo> EnumerateCombos()
    {
        for (int index = 0; index < _weights.Length; index++)
        {
            if (_weights[index] > 0)
            {
                yield return new WeightedCombo(HoleCards.FromIndex(index), _weights[index]);
            }
        }
    }

    public HandRange WithoutCards(ReadOnlySpan<Card> deadCards)
    {
        if (deadCards.Length == 0)
        {
            return this;
        }

        Span<bool> isDead = stackalloc bool[Card.Count];
        isDead.Clear();
        foreach (Card card in deadCards)
        {
            isDead[card.Index] = true;
        }

        double[] filtered = new double[HoleCards.Count];
        for (int index = 0; index < _weights.Length; index++)
        {
            if (_weights[index] <= 0)
            {
                continue;
            }

            HoleCards combo = HoleCards.FromIndex(index);
            if (!isDead[combo.First.Index] && !isDead[combo.Second.Index])
            {
                filtered[index] = _weights[index];
            }
        }

        return new HandRange(filtered);
    }

    /// <summary>Union: each combo keeps the greater of its two weights.</summary>
    public HandRange Union(HandRange other)
    {
        ArgumentNullException.ThrowIfNull(other);

        double[] union = new double[HoleCards.Count];
        for (int index = 0; index < _weights.Length; index++)
        {
            union[index] = Math.Max(_weights[index], other._weights[index]);
        }

        return new HandRange(union);
    }

    /// <summary>Difference: what is left after subtracting the other range's weight.</summary>
    public HandRange Except(HandRange other)
    {
        ArgumentNullException.ThrowIfNull(other);

        double[] difference = new double[HoleCards.Count];
        for (int index = 0; index < _weights.Length; index++)
        {
            difference[index] = Math.Max(0, _weights[index] - other._weights[index]);
        }

        return new HandRange(difference);
    }

    public HandRange Scaled(double factor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(factor);

        double[] scaled = new double[HoleCards.Count];
        for (int index = 0; index < _weights.Length; index++)
        {
            scaled[index] = Math.Clamp(_weights[index] * factor, 0, 1);
        }

        return new HandRange(scaled);
    }

    public double[] ToWeightArray()
    {
        return (double[])_weights.Clone();
    }

    public override string ToString()
    {
        return RangeNotationWriter.Write(this);
    }

    private static HandRange CreateFull()
    {
        double[] weights = new double[HoleCards.Count];
        Array.Fill(weights, 1.0);
        return new HandRange(weights);
    }
}
