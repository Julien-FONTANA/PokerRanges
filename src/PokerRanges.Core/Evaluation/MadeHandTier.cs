namespace PokerRanges.Core.Evaluation;

/// <summary>
/// A hand's strength relative to the board rather than in the absolute: pocket jacks are an
/// overpair on 7-4-2 and an underpair on A-K-Q, though the showdown category is the same.
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
