using Microsoft.Extensions.Logging.Abstractions;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Preflop;
using PokerRanges.Core.Ranges;
using PokerRanges.Data;
using Shouldly;

namespace PokerRanges.Data.Tests;

/// <summary>
/// Consistency checks on the shipped charts. They cannot say whether a range is "right" — that is
/// a player's judgement — but they catch everything that is mechanically wrong: unreadable
/// notation, frequencies above 100%, monotonicities the wrong way round.
/// </summary>
public sealed class PreflopChartDataTests
{
    private readonly JsonPreflopChartRepository _repository = new(
        PreflopChartRepositoryOptions.EmbeddedOnly,
        NullLogger<JsonPreflopChartRepository>.Instance);

    [Fact]
    public void EveryShippedChartIsLoadedAndReadable()
    {
        _repository.Charts.ShouldNotBeEmpty();

        foreach (PreflopChart chart in _repository.Charts)
        {
            Should.NotThrow(() => RangeStrategy.FromChart(chart), chart.Describe());
            chart.Actions.ShouldNotBeEmpty(chart.Describe());
            chart.Source.ShouldNotBeNullOrWhiteSpace(chart.Describe());
        }
    }

    [Fact]
    public void NoHandIsAskedToDoMoreThanOneHundredPercentOfTheTime()
    {
        foreach (PreflopChart chart in _repository.Charts)
        {
            RangeStrategy strategy = RangeStrategy.FromChart(chart);

            foreach (HoleCards combo in HoleCards.All())
            {
                double total = strategy.OptionsFor(combo).Sum(option => option.Frequency);

                total.ShouldBe(1, 1e-6, $"{chart.Describe()} — {combo}");
            }
        }
    }

    [Fact]
    public void EachChartKeyIsDefinedOnlyOnce()
    {
        IEnumerable<string> keys = _repository.Charts.Select(chart =>
            $"{chart.Context}|{chart.PlayersLeftToAct}|{chart.Relation}|{chart.DepthInBigBlinds}");

        keys.Distinct().Count().ShouldBe(_repository.Charts.Count);
    }

    /// <summary>
    /// The small blind is excluded on purpose: it has only one player behind, but it acts first on
    /// every postflop round, and that positional penalty outweighs the advantage of having few
    /// players left to get through. Its opening range is therefore legitimately tighter than the
    /// button's, which <see cref="TheSmallBlindOpensTighterThanTheButtonDespiteFewerPlayersBehind"/> verifies.
    /// </summary>
    [Theory]
    [InlineData(PreflopContext.RaiseFirstIn, 100)]
    [InlineData(PreflopContext.RaiseFirstIn, 25)]
    [InlineData(PreflopContext.Jam, 10)]
    public void TheRangeWidensAsFewerPlayersRemainToAct(PreflopContext context, double depth)
    {
        List<PreflopChart> charts =
        [
            .. _repository.Charts
                .Where(chart => chart.Context == context
                                && Math.Abs(chart.DepthInBigBlinds - depth) < 0.01
                                && chart.PlayersLeftToAct >= 2)
                .OrderByDescending(chart => chart.PlayersLeftToAct),
        ];

        charts.Count.ShouldBeGreaterThan(1);

        for (int index = 1; index < charts.Count; index++)
        {
            double earlier = ActiveCombos(charts[index - 1]);
            double later = ActiveCombos(charts[index]);

            later.ShouldBeGreaterThan(
                earlier,
                $"{charts[index].Describe()} devrait être plus large que {charts[index - 1].Describe()}");
        }
    }

    [Theory]
    [InlineData(100)]
    [InlineData(25)]
    public void TheSmallBlindOpensTighterThanTheButtonDespiteFewerPlayersBehind(double depth)
    {
        double smallBlind = ActiveCombos(OpeningChart(depth, playersLeftToAct: 1));
        double button = ActiveCombos(OpeningChart(depth, playersLeftToAct: 2));

        smallBlind.ShouldBeLessThan(button);
        smallBlind.ShouldBeGreaterThan(ActiveCombos(OpeningChart(depth, playersLeftToAct: 3)));
    }

    [Fact]
    public void TheOpeningPercentagesStayInAPlausibleBand()
    {
        foreach (PreflopChart chart in _repository.Charts.Where(chart => chart.Context == PreflopContext.RaiseFirstIn))
        {
            double percent = ActiveCombos(chart) * 100.0 / HoleCards.Count;

            percent.ShouldBeInRange(8, 55, chart.Describe());
        }
    }

    [Fact]
    public void ADepthWithoutItsOwnChartSnapsToTheNearestOneAndSaysSo()
    {
        ChartResolution resolution = _repository.Resolve(
            new ChartKey(PreflopContext.RaiseFirstIn, 3, null, 125));

        resolution.Chart.DepthInBigBlinds.ShouldBe(100);
        resolution.IsExactMatch.ShouldBeFalse();
        resolution.Adjustments.ShouldContain(adjustment => adjustment.Contains("125", StringComparison.Ordinal));
        resolution.Describe().ShouldContain("100");
    }

    [Fact]
    public void AnExactMatchIsReportedAsSuch()
    {
        ChartResolution resolution = _repository.Resolve(
            new ChartKey(PreflopContext.RaiseFirstIn, 3, null, 100));

        resolution.IsExactMatch.ShouldBeTrue();
        resolution.Chart.PlayersLeftToAct.ShouldBe(3);
    }

    [Fact]
    public void AContextWithoutAnyChartFallsBackAndWarnsLoudly()
    {
        ChartResolution resolution = _repository.Resolve(
            new ChartKey(PreflopContext.VersusFourBet, 3, FacingRelation.InPosition, 100));

        resolution.IsExactMatch.ShouldBeFalse();
        resolution.Adjustments.ShouldContain(adjustment => adjustment.Contains("caution", StringComparison.Ordinal));
    }

    private PreflopChart OpeningChart(double depth, int playersLeftToAct)
    {
        return _repository.Charts.Single(chart =>
            chart.Context == PreflopContext.RaiseFirstIn
            && chart.PlayersLeftToAct == playersLeftToAct
            && Math.Abs(chart.DepthInBigBlinds - depth) < 0.01);
    }

    private static double ActiveCombos(PreflopChart chart)
    {
        return chart.Actions.Sum(action => RangeNotationParser.Parse(action.Range).TotalCombos);
    }
}
