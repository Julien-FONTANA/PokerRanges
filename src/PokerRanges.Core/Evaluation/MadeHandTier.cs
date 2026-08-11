namespace PokerRanges.Core.Evaluation;

/// <summary>
/// La force d'une main relativement au board, et non dans l'absolu : une paire de valets vaut une
/// overpaire sur 7-4-2 et une sous-paire sur A-K-Q, alors que la catégorie abattue est la même.
/// </summary>
public enum MadeHandTier
{
    HighCard = 0,
    UnderPair = 1,
    BottomPair = 2,
    MiddlePair = 3,
    TopPair = 4,
    Overpair = 5,
    TwoPair = 6,
    Trips = 7,
    Set = 8,
    Straight = 9,
    Flush = 10,
    FullHouse = 11,
    Quads = 12,
    StraightFlush = 13,
}
