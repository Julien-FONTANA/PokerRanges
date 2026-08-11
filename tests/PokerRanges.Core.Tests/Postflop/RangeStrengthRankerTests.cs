using Microsoft.Extensions.Logging.Abstractions;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Evaluation;
using PokerRanges.Core.Postflop;
using PokerRanges.Core.Ranges;
using Shouldly;

namespace PokerRanges.Core.Tests.Postflop;

public sealed class RangeStrengthRankerTests
{
    private static readonly PostflopBudget TestBudget = PostflopBudget.Full with
    {
        RankingSamplesPerCombo = 120,
    };

    private readonly RangeStrengthRanker _ranker = new(
        new RankCountHandEvaluator(),
        PostflopOptions.Default,
        NullLogger<RangeStrengthRanker>.Instance);

    [Fact]
    public void TheRangeComesBackSortedFromStrongestToWeakest()
    {
        IReadOnlyList<RankedCombo> ranked = Rank("22+, A2s+, KQo, 72o", "Ks7d2c");

        ranked.ShouldNotBeEmpty();
        for (int index = 1; index < ranked.Count; index++)
        {
            ranked[index].Equity.ShouldBeLessThanOrEqualTo(ranked[index - 1].Equity);
        }
    }

    [Fact]
    public void ASetOutranksAnOverpairWhichOutranksAirOnTheSameBoard()
    {
        IReadOnlyList<RankedCombo> ranked = Rank("22+, AKo, QJo", "Ks7d2c");

        EquityOf(ranked, "2h2d").ShouldBeGreaterThan(EquityOf(ranked, "AhAd"));
        EquityOf(ranked, "AhAd").ShouldBeGreaterThan(EquityOf(ranked, "QhJd"));
    }

    [Fact]
    public void AFlushDrawIsRatedAboveABareAirHand()
    {
        IReadOnlyList<RankedCombo> ranked = Rank("A2s+, 33, QJo", "Ks7s2d");

        EquityOf(ranked, "As4s").ShouldBeGreaterThan(EquityOf(ranked, "QhJd"));
    }

    [Fact]
    public void TheBoardCardsAreRemovedFromTheRange()
    {
        IReadOnlyList<RankedCombo> ranked = Rank("AA", "AsKd2c");

        ranked.Count.ShouldBe(3);
        ranked.ShouldAllBe(entry => !entry.Combo.Contains(Card.Parse("As")));
    }

    [Fact]
    public void TheSameSituationAlwaysGivesTheSameRanking()
    {
        IReadOnlyList<RankedCombo> first = Rank("22+, A2s+", "Ks7d2c");
        IReadOnlyList<RankedCombo> second = Rank("22+, A2s+", "Ks7d2c");

        second.Select(entry => entry.Combo).ShouldBe(first.Select(entry => entry.Combo));
        second.Select(entry => entry.Equity).ShouldBe(first.Select(entry => entry.Equity));
    }

    [Fact]
    public void AnEmptyRangeProducesAnEmptyRanking()
    {
        Rank("AA", "AsAhAd").ShouldBeEmpty();
    }

    private static double EquityOf(IReadOnlyList<RankedCombo> ranked, string combo)
    {
        HoleCards target = HoleCards.Parse(combo);
        return ranked.Single(entry => entry.Combo == target).Equity;
    }

    private IReadOnlyList<RankedCombo> Rank(string notation, string board)
    {
        return _ranker.Rank(
            RangeNotationParser.Parse(notation),
            TestCards.Parse(board),
            [],
            TestBudget,
            TestContext.Current.CancellationToken);
    }
}
