using System.Collections.Immutable;
using System.Globalization;
using PokerRanges.Core.Cards;

namespace PokerRanges.Core.Ranges;

/// <summary>
/// Reads the range notation standard to poker tools: "77+, ATs+, KQo, A5s-A2s, AsKh:0.5".
/// This is the format the JSON charts are written and read back in.
/// The "+" walks the kicker up while keeping the high card ("QJs+" means QJs only), as in
/// PokerStove, Equilab and GTO+; runs of connectors are written as bounded ranges ("AKs-QJs").
/// </summary>
public static class RangeNotationParser
{
    private static readonly char[] Separators = [',', ';', ' ', '\t', '\r', '\n'];

    private static readonly ImmutableArray<HandShape> BothShapes =
        [HandShape.Suited, HandShape.Offsuit];

    private static readonly ImmutableArray<HandShape> SuitedOnly = [HandShape.Suited];

    private static readonly ImmutableArray<HandShape> OffsuitOnly = [HandShape.Offsuit];

    public static HandRange Parse(string notation)
    {
        ArgumentNullException.ThrowIfNull(notation);

        HandRangeBuilder builder = new();
        foreach (string token in notation.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            ApplyToken(token, builder);
        }

        return builder.Build();
    }

    public static bool TryParse(string notation, out HandRange range)
    {
        try
        {
            range = Parse(notation);
            return true;
        }
        catch (RangeNotationException)
        {
            range = HandRange.Empty;
            return false;
        }
    }

    private static void ApplyToken(string token, HandRangeBuilder builder)
    {
        string body = token;
        double weight = 1.0;

        int weightSeparator = token.IndexOf(':', StringComparison.Ordinal);
        if (weightSeparator >= 0)
        {
            body = token[..weightSeparator];
            string weightText = token[(weightSeparator + 1)..];

            if (!double.TryParse(weightText, NumberStyles.Float, CultureInfo.InvariantCulture, out weight)
                || weight < 0
                || weight > 1)
            {
                throw new RangeNotationException(token, "le poids doit être un nombre entre 0 et 1, par exemple « AKo:0.5 ».");
            }
        }

        if (body.Length == 0)
        {
            throw new RangeNotationException(token, "élément vide.");
        }

        if (HoleCards.TryParse(body, out HoleCards combo))
        {
            builder.Set(combo, weight);
            return;
        }

        if (body.EndsWith('+'))
        {
            ApplyOpenEnded(token, body[..^1], weight, builder);
            return;
        }

        int dashIndex = body.IndexOf('-', StringComparison.Ordinal);
        if (dashIndex > 0)
        {
            ApplyBounded(token, body[..dashIndex], body[(dashIndex + 1)..], weight, builder);
            return;
        }

        ParseToken(token, body, out Rank high, out Rank low, out HandShape? shape);
        ApplySingle(high, low, shape, weight, builder);
    }

    private static void ApplySingle(Rank high, Rank low, HandShape? shape, double weight, HandRangeBuilder builder)
    {
        if (shape == HandShape.Pair)
        {
            builder.Set(HandClass.Pair(high), weight);
            return;
        }

        foreach (HandShape resolved in ShapesFor(shape))
        {
            builder.Set(new HandClass(high, low, resolved), weight);
        }
    }

    private static void ApplyOpenEnded(string token, string body, double weight, HandRangeBuilder builder)
    {
        ParseToken(token, body, out Rank high, out Rank low, out HandShape? shape);

        if (shape == HandShape.Pair)
        {
            for (int rank = (int)high; rank <= (int)Rank.Ace; rank++)
            {
                builder.Set(HandClass.Pair((Rank)rank), weight);
            }

            return;
        }

        for (int lowRank = (int)low; lowRank < (int)high; lowRank++)
        {
            foreach (HandShape resolved in ShapesFor(shape))
            {
                builder.Set(new HandClass(high, (Rank)lowRank, resolved), weight);
            }
        }
    }

    private static void ApplyBounded(string token, string leftBody, string rightBody, double weight, HandRangeBuilder builder)
    {
        ParseToken(token, leftBody, out Rank leftHigh, out Rank leftLow, out HandShape? leftShape);
        ParseToken(token, rightBody, out Rank rightHigh, out Rank rightLow, out HandShape? rightShape);

        if (leftShape != rightShape)
        {
            throw new RangeNotationException(token, "les deux bornes doivent être du même type (paire, assorti ou dépareillé).");
        }

        if (leftShape == HandShape.Pair)
        {
            int fromPair = Math.Min((int)leftHigh, (int)rightHigh);
            int toPair = Math.Max((int)leftHigh, (int)rightHigh);
            for (int rank = fromPair; rank <= toPair; rank++)
            {
                builder.Set(HandClass.Pair((Rank)rank), weight);
            }

            return;
        }

        if (leftHigh == rightHigh)
        {
            int fromLow = Math.Min((int)leftLow, (int)rightLow);
            int toLow = Math.Max((int)leftLow, (int)rightLow);
            for (int lowRank = fromLow; lowRank <= toLow; lowRank++)
            {
                foreach (HandShape resolved in ShapesFor(leftShape))
                {
                    builder.Set(new HandClass(leftHigh, (Rank)lowRank, resolved), weight);
                }
            }

            return;
        }

        int gap = (int)leftHigh - (int)leftLow;
        if (gap != (int)rightHigh - (int)rightLow)
        {
            throw new RangeNotationException(token, "les deux bornes doivent partager la même carte haute (« A5s-A2s ») ou le même écart (« 98s-65s »).");
        }

        int fromHigh = Math.Min((int)leftHigh, (int)rightHigh);
        int toHigh = Math.Max((int)leftHigh, (int)rightHigh);
        for (int highRank = fromHigh; highRank <= toHigh; highRank++)
        {
            foreach (HandShape resolved in ShapesFor(leftShape))
            {
                builder.Set(new HandClass((Rank)highRank, (Rank)(highRank - gap), resolved), weight);
            }
        }
    }

    private static void ParseToken(string token, string body, out Rank high, out Rank low, out HandShape? shape)
    {
        if (body.Length is not (2 or 3)
            || !CardSymbols.TryParseRank(body[0], out Rank first)
            || !CardSymbols.TryParseRank(body[1], out Rank second))
        {
            throw new RangeNotationException(token, "attendu deux rangs, éventuellement suivis de « s » ou « o », par exemple « AKs », « T9o » ou « QQ ».");
        }

        high = first > second ? first : second;
        low = first > second ? second : first;

        if (body.Length == 2)
        {
            shape = first == second ? HandShape.Pair : null;
            return;
        }

        if (first == second)
        {
            throw new RangeNotationException(token, "une paire ne peut pas être suffixée par « s » ou « o ».");
        }

        shape = char.ToLowerInvariant(body[2]) switch
        {
            's' => HandShape.Suited,
            'o' => HandShape.Offsuit,
            _ => throw new RangeNotationException(token, "le suffixe doit être « s » (assorti) ou « o » (dépareillé)."),
        };
    }

    private static ImmutableArray<HandShape> ShapesFor(HandShape? shape)
    {
        return shape switch
        {
            HandShape.Suited => SuitedOnly,
            HandShape.Offsuit => OffsuitOnly,
            _ => BothShapes,
        };
    }
}
