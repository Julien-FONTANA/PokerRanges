using PokerRanges.Core.Cards;

namespace PokerRanges.Core.Ranges;

/// <summary>
/// Une range immuable : un poids de 0 à 1 pour chacun des 1326 combos. Le poids par combo, et non
/// par case de la grille, est ce qui permet de retirer proprement les combos bloqués par le board
/// et par les cartes du héros.
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
    /// Part de la case effectivement contenue dans la range, de 0 à 1 : c'est le remplissage
    /// affiché par la grille 13x13.
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

    /// <summary>Réunion : chaque combo garde le plus fort de ses deux poids.</summary>
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

    /// <summary>Différence : ce qui reste après avoir retiré le poids de l'autre range.</summary>
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
