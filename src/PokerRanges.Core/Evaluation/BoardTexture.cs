using PokerRanges.Core.Cards;
using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Evaluation;

/// <summary>
/// Ce que le board rend possible. L'humidité résume en un nombre de 0 à 1 le danger qu'il
/// représente : un board sec laisse peu de tirages, un board humide en autorise beaucoup, ce qui
/// change la taille de mise rentable et la vitesse à laquelle une range se resserre.
/// </summary>
public sealed record BoardTexture
{
    public required int CardCount { get; init; }

    public required bool IsPaired { get; init; }

    public required bool HasTrips { get; init; }

    /// <summary>Trois cartes ou plus de la même couleur : une couleur est déjà possible.</summary>
    public required bool IsMonotone { get; init; }

    public required bool IsTwoTone { get; init; }

    public required bool IsRainbow { get; init; }

    public required bool AllowsFlush { get; init; }

    public required bool AllowsFlushDraw { get; init; }

    /// <summary>Nombre de quintes différentes qu'une main de deux cartes peut encore composer.</summary>
    public required int StraightWindows { get; init; }

    public required Rank HighCard { get; init; }

    public required double Wetness { get; init; }

    public bool AllowsStraight => StraightWindows > 0;

    public static BoardTexture Read(ReadOnlySpan<Card> board)
    {
        if (board.Length is < 3 or > 5)
        {
            throw new ArgumentException(HandText.BoardNeedsThreeToFiveCards(board.Length), nameof(board));
        }

        Span<int> rankCounts = stackalloc int[15];
        Span<int> suitCounts = stackalloc int[4];
        rankCounts.Clear();
        suitCounts.Clear();

        Rank highCard = Rank.Two;
        foreach (Card card in board)
        {
            rankCounts[(int)card.Rank]++;
            suitCounts[(int)card.Suit]++;
            highCard = card.Rank > highCard ? card.Rank : highCard;
        }

        int topSuitCount = 0;
        foreach (int count in suitCounts)
        {
            topSuitCount = Math.Max(topSuitCount, count);
        }

        int topRankCount = 0;
        foreach (int count in rankCounts)
        {
            topRankCount = Math.Max(topRankCount, count);
        }

        int straightWindows = CountStraightWindows(rankCounts);

        return new BoardTexture
        {
            CardCount = board.Length,
            IsPaired = topRankCount >= 2,
            HasTrips = topRankCount >= 3,
            IsMonotone = topSuitCount >= 3,
            IsTwoTone = topSuitCount == 2,
            IsRainbow = topSuitCount == 1,
            AllowsFlush = topSuitCount >= 3,
            AllowsFlushDraw = board.Length < 5 && topSuitCount >= 2,
            StraightWindows = straightWindows,
            HighCard = highCard,
            Wetness = ComputeWetness(topSuitCount, straightWindows, topRankCount),
        };
    }

    public string Describe()
    {
        List<string> traits = [];

        if (HasTrips)
        {
            traits.Add(HandText.BoardTrips);
        }
        else if (IsPaired)
        {
            traits.Add(HandText.BoardPaired);
        }

        traits.Add(IsMonotone ? HandText.BoardMonotone : IsTwoTone ? HandText.BoardTwoTone : HandText.BoardRainbow);

        if (AllowsStraight)
        {
            traits.Add(StraightWindows >= 3 ? HandText.BoardVeryConnected : HandText.BoardConnected);
        }

        traits.Add(Wetness >= 0.6 ? HandText.BoardWet : Wetness >= 0.3 ? HandText.BoardSemiWet : HandText.BoardDry);

        return HandText.BoardSummary(HighCard, string.Join(", ", traits));
    }

    /// <summary>
    /// Compte les fenêtres de cinq rangs consécutifs auxquelles le board apporte déjà au moins
    /// trois cartes : ce sont les quintes qu'une main de deux cartes peut encore compléter.
    /// </summary>
    private static int CountStraightWindows(ReadOnlySpan<int> rankCounts)
    {
        int windows = 0;

        for (int low = 1; low <= 10; low++)
        {
            int present = 0;

            for (int offset = 0; offset < 5; offset++)
            {
                int rank = low + offset;
                int normalised = rank == 1 ? (int)Rank.Ace : rank;

                if (rankCounts[normalised] > 0)
                {
                    present++;
                }
            }

            if (present >= 3)
            {
                windows++;
            }
        }

        return windows;
    }

    private static double ComputeWetness(int topSuitCount, int straightWindows, int topRankCount)
    {
        double flushPressure = topSuitCount >= 3 ? 1.0 : topSuitCount == 2 ? 0.5 : 0;
        double straightPressure = Math.Min(1.0, straightWindows / 3.0);
        double pairRelief = topRankCount >= 2 ? 0.15 : 0;

        return Math.Clamp((flushPressure * 0.5) + (straightPressure * 0.5) - pairRelief, 0, 1);
    }
}
