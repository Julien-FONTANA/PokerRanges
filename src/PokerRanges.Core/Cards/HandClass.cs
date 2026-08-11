using System.Collections.Immutable;

namespace PokerRanges.Core.Cards;

/// <summary>
/// One of the 169 cells of the grid: a pair, two suited ranks, or two offsuit ranks. This is the
/// unit charts are displayed and written in; the calculations themselves work in
/// <see cref="HoleCards"/>, because only the exact combo can account for cards the board blocks.
/// </summary>
public readonly record struct HandClass
{
    public const int Count = 169;
    public const int GridSize = 13;

    public HandClass(Rank high, Rank low, HandShape shape)
    {
        if (shape == HandShape.Pair)
        {
            if (high != low)
            {
                throw new ArgumentException($"Une paire exige deux rangs identiques, reçu {high} et {low}.", nameof(shape));
            }
        }
        else if (high == low)
        {
            throw new ArgumentException($"Deux rangs identiques ne peuvent pas former une main {shape}.", nameof(shape));
        }

        High = high > low ? high : low;
        Low = high > low ? low : high;
        Shape = shape;
    }

    public Rank High { get; }

    public Rank Low { get; }

    public HandShape Shape { get; }

    public static ImmutableArray<HandClass> All { get; } = BuildAll();

    public int CombinationCount => Shape switch
    {
        HandShape.Pair => 6,
        HandShape.Suited => 4,
        _ => 12,
    };

    public int GridRow => Shape == HandShape.Offsuit ? GridIndexOf(Low) : GridIndexOf(High);

    public int GridColumn => Shape == HandShape.Offsuit ? GridIndexOf(High) : GridIndexOf(Low);

    public static HandClass Pair(Rank rank)
    {
        return new HandClass(rank, rank, HandShape.Pair);
    }

    public static HandClass Suited(Rank high, Rank low)
    {
        return new HandClass(high, low, HandShape.Suited);
    }

    public static HandClass Offsuit(Rank high, Rank low)
    {
        return new HandClass(high, low, HandShape.Offsuit);
    }

    public static HandClass FromGrid(int row, int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, GridSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, GridSize);

        Rank rowRank = Deck.RanksHighToLow[row];
        Rank columnRank = Deck.RanksHighToLow[column];

        if (row == column)
        {
            return Pair(rowRank);
        }

        return row < column ? Suited(rowRank, columnRank) : Offsuit(columnRank, rowRank);
    }

    public static HandClass Parse(ReadOnlySpan<char> text)
    {
        if (!TryParse(text, out HandClass handClass))
        {
            throw new CardFormatException($"Main invalide : « {text} ». Format attendu : deux rangs, suivis de « s » ou « o » hors paire, par exemple « AKs », « T9o » ou « QQ ».");
        }

        return handClass;
    }

    public static bool TryParse(ReadOnlySpan<char> text, out HandClass handClass)
    {
        handClass = default;
        ReadOnlySpan<char> trimmed = text.Trim();

        if (trimmed.Length is not (2 or 3)
            || !CardSymbols.TryParseRank(trimmed[0], out Rank first)
            || !CardSymbols.TryParseRank(trimmed[1], out Rank second))
        {
            return false;
        }

        if (first == second)
        {
            if (trimmed.Length != 2)
            {
                return false;
            }

            handClass = Pair(first);
            return true;
        }

        if (trimmed.Length != 3)
        {
            return false;
        }

        HandShape shape = char.ToLowerInvariant(trimmed[2]) switch
        {
            's' => HandShape.Suited,
            'o' => HandShape.Offsuit,
            _ => (HandShape)(-1),
        };

        if (shape is not (HandShape.Suited or HandShape.Offsuit))
        {
            return false;
        }

        handClass = new HandClass(first, second, shape);
        return true;
    }

    public IEnumerable<HoleCards> Combos()
    {
        if (Shape == HandShape.Pair)
        {
            for (int firstSuit = 0; firstSuit < 4; firstSuit++)
            {
                for (int secondSuit = firstSuit + 1; secondSuit < 4; secondSuit++)
                {
                    yield return new HoleCards(
                        new Card(High, (Suit)firstSuit),
                        new Card(Low, (Suit)secondSuit));
                }
            }

            yield break;
        }

        if (Shape == HandShape.Suited)
        {
            foreach (Suit suit in Deck.AllSuits)
            {
                yield return new HoleCards(new Card(High, suit), new Card(Low, suit));
            }

            yield break;
        }

        foreach (Suit highSuit in Deck.AllSuits)
        {
            foreach (Suit lowSuit in Deck.AllSuits)
            {
                if (highSuit != lowSuit)
                {
                    yield return new HoleCards(new Card(High, highSuit), new Card(Low, lowSuit));
                }
            }
        }
    }

    public override string ToString()
    {
        char high = CardSymbols.ToCharacter(High);
        char low = CardSymbols.ToCharacter(Low);

        return Shape == HandShape.Pair
            ? new string([high, low])
            : new string([high, low, CardSymbols.ToCharacter(Shape)]);
    }

    private static int GridIndexOf(Rank rank)
    {
        return (int)Rank.Ace - (int)rank;
    }

    private static ImmutableArray<HandClass> BuildAll()
    {
        ImmutableArray<HandClass>.Builder builder = ImmutableArray.CreateBuilder<HandClass>(Count);
        for (int row = 0; row < GridSize; row++)
        {
            for (int column = 0; column < GridSize; column++)
            {
                builder.Add(FromGrid(row, column));
            }
        }

        return builder.MoveToImmutable();
    }
}
