using System.Globalization;
using System.Text;
using PokerRanges.Core.Cards;

namespace PokerRanges.Core.Ranges;

/// <summary>
/// Writes a range back in standard notation, grouping runs of hands ("77+", "A5s-A2s"). Cells
/// whose combos do not all share the same weight are listed combo by combo, for want of a compact
/// notation able to describe them.
/// </summary>
public static class RangeNotationWriter
{
    private const double Tolerance = 1e-9;

    public static string Write(HandRange range)
    {
        ArgumentNullException.ThrowIfNull(range);

        Dictionary<HandClass, double> uniformWeights = [];
        List<HandClass> mixedClasses = [];

        foreach (HandClass handClass in HandClass.All)
        {
            if (!TryGetUniformWeight(range, handClass, out double weight))
            {
                mixedClasses.Add(handClass);
                continue;
            }

            if (weight > Tolerance)
            {
                uniformWeights[handClass] = weight;
            }
        }

        List<string> parts = [];
        AppendPairs(uniformWeights, parts);
        AppendNonPairs(uniformWeights, HandShape.Suited, parts);
        AppendNonPairs(uniformWeights, HandShape.Offsuit, parts);
        AppendMixedCombos(range, mixedClasses, parts);

        return string.Join(", ", parts);
    }

    private static bool TryGetUniformWeight(HandRange range, HandClass handClass, out double weight)
    {
        weight = 0;
        bool isFirst = true;

        foreach (HoleCards combo in handClass.Combos())
        {
            double current = range.GetWeight(combo);

            if (isFirst)
            {
                weight = current;
                isFirst = false;
                continue;
            }

            if (Math.Abs(current - weight) > Tolerance)
            {
                weight = 0;
                return false;
            }
        }

        return true;
    }

    private static void AppendPairs(Dictionary<HandClass, double> uniformWeights, List<string> parts)
    {
        int rank = (int)Rank.Two;

        while (rank <= (int)Rank.Ace)
        {
            if (!uniformWeights.TryGetValue(HandClass.Pair((Rank)rank), out double weight))
            {
                rank++;
                continue;
            }

            int startRank = rank;
            while (rank + 1 <= (int)Rank.Ace
                   && uniformWeights.TryGetValue(HandClass.Pair((Rank)(rank + 1)), out double nextWeight)
                   && Math.Abs(nextWeight - weight) <= Tolerance)
            {
                rank++;
            }

            parts.Add(FormatPairRun(startRank, rank, weight));
            rank++;
        }
    }

    private static void AppendNonPairs(
        Dictionary<HandClass, double> uniformWeights,
        HandShape shape,
        List<string> parts)
    {
        foreach (Rank high in Deck.RanksHighToLow)
        {
            int low = (int)Rank.Two;

            while (low < (int)high)
            {
                if (!uniformWeights.TryGetValue(new HandClass(high, (Rank)low, shape), out double weight))
                {
                    low++;
                    continue;
                }

                int startLow = low;
                while (low + 1 < (int)high
                       && uniformWeights.TryGetValue(new HandClass(high, (Rank)(low + 1), shape), out double nextWeight)
                       && Math.Abs(nextWeight - weight) <= Tolerance)
                {
                    low++;
                }

                parts.Add(FormatNonPairRun(high, startLow, low, shape, weight));
                low++;
            }
        }
    }

    private static void AppendMixedCombos(HandRange range, List<HandClass> mixedClasses, List<string> parts)
    {
        foreach (HandClass handClass in mixedClasses)
        {
            foreach (HoleCards combo in handClass.Combos())
            {
                double weight = range.GetWeight(combo);
                if (weight > Tolerance)
                {
                    parts.Add(combo + FormatWeight(weight));
                }
            }
        }
    }

    private static string FormatPairRun(int startRank, int endRank, double weight)
    {
        char start = CardSymbols.ToCharacter((Rank)startRank);
        char end = CardSymbols.ToCharacter((Rank)endRank);

        StringBuilder text = new();
        text.Append(start).Append(start);

        if (startRank != endRank)
        {
            if (endRank == (int)Rank.Ace)
            {
                text.Append('+');
            }
            else
            {
                text.Append('-').Append(end).Append(end);
            }
        }

        return text.Append(FormatWeight(weight)).ToString();
    }

    private static string FormatNonPairRun(Rank high, int startLow, int endLow, HandShape shape, double weight)
    {
        char highCharacter = CardSymbols.ToCharacter(high);
        char shapeCharacter = CardSymbols.ToCharacter(shape);

        StringBuilder text = new();
        text.Append(highCharacter).Append(CardSymbols.ToCharacter((Rank)startLow)).Append(shapeCharacter);

        if (startLow != endLow)
        {
            if (endLow == (int)high - 1)
            {
                text.Append('+');
            }
            else
            {
                text.Append('-')
                    .Append(highCharacter)
                    .Append(CardSymbols.ToCharacter((Rank)endLow))
                    .Append(shapeCharacter);
            }
        }

        return text.Append(FormatWeight(weight)).ToString();
    }

    private static string FormatWeight(double weight)
    {
        return weight >= 1 - Tolerance
            ? string.Empty
            : ":" + weight.ToString("0.####", CultureInfo.InvariantCulture);
    }
}
