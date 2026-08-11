using PokerRanges.Core.Cards;
using PokerRanges.Core.Evaluation;
using Shouldly;

namespace PokerRanges.Core.Tests.Evaluation;

/// <summary>
/// Validation exhaustive de l'évaluateur. Les fréquences des neuf catégories sur les 2 598 960
/// mains de cinq cartes sont des constantes combinatoires connues : les retrouver toutes ne laisse
/// pratiquement aucune place à une erreur de classement.
/// </summary>
public sealed class HandEvaluatorFrequencyTests
{
    private readonly RankCountHandEvaluator _evaluator = new();

    [Fact]
    public void EveryFiveCardHandOfTheDeckFallsIntoItsKnownFrequency()
    {
        Dictionary<HandCategory, int> counts = [];
        Card[] hand = new Card[5];

        for (int first = 0; first < Card.Count; first++)
        {
            hand[0] = Card.FromIndex(first);
            for (int second = first + 1; second < Card.Count; second++)
            {
                hand[1] = Card.FromIndex(second);
                for (int third = second + 1; third < Card.Count; third++)
                {
                    hand[2] = Card.FromIndex(third);
                    for (int fourth = third + 1; fourth < Card.Count; fourth++)
                    {
                        hand[3] = Card.FromIndex(fourth);
                        for (int fifth = fourth + 1; fifth < Card.Count; fifth++)
                        {
                            hand[4] = Card.FromIndex(fifth);
                            HandCategory category = _evaluator.Evaluate(hand).Category;
                            counts[category] = counts.GetValueOrDefault(category) + 1;
                        }
                    }
                }
            }
        }

        counts[HandCategory.StraightFlush].ShouldBe(40);
        counts[HandCategory.FourOfAKind].ShouldBe(624);
        counts[HandCategory.FullHouse].ShouldBe(3_744);
        counts[HandCategory.Flush].ShouldBe(5_108);
        counts[HandCategory.Straight].ShouldBe(10_200);
        counts[HandCategory.ThreeOfAKind].ShouldBe(54_912);
        counts[HandCategory.TwoPair].ShouldBe(123_552);
        counts[HandCategory.OnePair].ShouldBe(1_098_240);
        counts[HandCategory.HighCard].ShouldBe(1_302_540);
        counts.Values.Sum().ShouldBe(2_598_960);
    }

    [Fact]
    public void SevenCardEvaluationMatchesTheBestOfItsTwentyOneFiveCardSubsets()
    {
        Random random = new(20260731);
        Card[] hand = new Card[7];
        bool[] used = new bool[Card.Count];

        for (int iteration = 0; iteration < 50_000; iteration++)
        {
            Array.Clear(used);
            for (int position = 0; position < hand.Length; position++)
            {
                int index;
                do
                {
                    index = random.Next(Card.Count);
                }
                while (used[index]);

                used[index] = true;
                hand[position] = Card.FromIndex(index);
            }

            HandValue direct = _evaluator.Evaluate(hand);
            HandValue reference = BestOfFiveCardSubsets(hand);

            direct.ShouldBe(reference, $"main {string.Join(string.Empty, hand.Select(card => card.ToString()))}");
        }
    }

    private HandValue BestOfFiveCardSubsets(ReadOnlySpan<Card> sevenCards)
    {
        HandValue best = default;
        Span<Card> subset = stackalloc Card[5];

        for (int excludedFirst = 0; excludedFirst < sevenCards.Length; excludedFirst++)
        {
            for (int excludedSecond = excludedFirst + 1; excludedSecond < sevenCards.Length; excludedSecond++)
            {
                int written = 0;
                for (int index = 0; index < sevenCards.Length; index++)
                {
                    if (index != excludedFirst && index != excludedSecond)
                    {
                        subset[written] = sevenCards[index];
                        written++;
                    }
                }

                HandValue value = _evaluator.Evaluate(subset);
                if (value > best)
                {
                    best = value;
                }
            }
        }

        return best;
    }
}
