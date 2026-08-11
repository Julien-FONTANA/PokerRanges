using PokerRanges.Core.Cards;

namespace PokerRanges.Core.Ranges;

public sealed class HandRangeBuilder
{
    private readonly double[] _weights = new double[HoleCards.Count];

    public HandRangeBuilder Set(HoleCards combo, double weight)
    {
        _weights[combo.Index] = Math.Clamp(weight, 0, 1);
        return this;
    }

    public HandRangeBuilder Set(HandClass handClass, double weight)
    {
        double clamped = Math.Clamp(weight, 0, 1);
        foreach (HoleCards combo in handClass.Combos())
        {
            _weights[combo.Index] = clamped;
        }

        return this;
    }

    public HandRangeBuilder SetAll(double weight)
    {
        Array.Fill(_weights, Math.Clamp(weight, 0, 1));
        return this;
    }

    public double GetWeight(HoleCards combo)
    {
        return _weights[combo.Index];
    }

    public HandRange Build()
    {
        return new HandRange((double[])_weights.Clone());
    }
}
