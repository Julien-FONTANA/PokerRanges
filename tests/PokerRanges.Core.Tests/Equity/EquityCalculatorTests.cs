using Microsoft.Extensions.Logging.Abstractions;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Equity;
using PokerRanges.Core.Evaluation;
using PokerRanges.Core.Ranges;
using Shouldly;

namespace PokerRanges.Core.Tests.Equity;

public sealed class EquityCalculatorTests
{
    private readonly EquityCalculator _calculator = new(
        new RankCountHandEvaluator(),
        NullLogger<EquityCalculator>.Instance);

    [Fact]
    public async Task AcesAgainstKingsLandOnTheirPublishedEquity()
    {
        EquityResult result = await ComputeAsync(EquityRequest.Between(Range("AsAh"), Range("KsKh")));

        result.WasExhaustive.ShouldBeTrue();
        result.Hero.Equity.ShouldBe(0.8236, 0.004);
        result.Players[1].Equity.ShouldBe(1 - result.Hero.Equity, 1e-9);
    }

    [Fact]
    public async Task ASuitedBroadwayAgainstAnOverpairIsCloseToACoinFlip()
    {
        EquityResult result = await ComputeAsync(EquityRequest.Between(Range("AsKs"), Range("QhQd")));

        result.Hero.Equity.ShouldBe(0.462, 0.006);
    }

    [Fact]
    public async Task AcesAgainstARandomHandKeepTheirDominance()
    {
        EquityResult result = await ComputeAsync(new EquityRequest
        {
            PlayerRanges = [Range("AsAh"), HandRange.Full],
            RandomSeed = 7,
            MaximumSamples = 120_000,
        });

        result.Hero.Equity.ShouldBeInRange(0.83, 0.88);
    }

    [Fact]
    public async Task TwoIdenticalRangesSplitTheEquityEvenly()
    {
        EquityResult result = await ComputeAsync(new EquityRequest
        {
            PlayerRanges = [Range("AA"), Range("AA")],
            Board = TestCards.Parse("Kh7d2c9s"),
        });

        result.WasExhaustive.ShouldBeTrue();
        result.Hero.Equity.ShouldBe(0.5, 1e-9);
        result.Players[1].Equity.ShouldBe(0.5, 1e-9);
    }

    [Fact]
    public async Task ACompletedBoardIsDecidedWithoutAnyRandomness()
    {
        EquityResult result = await ComputeAsync(new EquityRequest
        {
            PlayerRanges = [Range("AsKs"), Range("QhQd")],
            Board = TestCards.Parse("JsTs2s7d3h"),
        });

        result.WasExhaustive.ShouldBeTrue();
        result.SamplesEvaluated.ShouldBe(1);
        result.Hero.Equity.ShouldBe(1, 1e-9);
        result.Hero.WinRate.ShouldBe(1, 1e-9);
        result.Players[1].Equity.ShouldBe(0, 1e-9);
    }

    [Fact]
    public async Task ATiedBoardIsSharedBetweenTheTwoPlayers()
    {
        EquityResult result = await ComputeAsync(new EquityRequest
        {
            PlayerRanges = [Range("2c2d"), Range("2h2s")],
            Board = TestCards.Parse("AsAhAdKcKh"),
        });

        result.Hero.Equity.ShouldBe(0.5, 1e-9);
        result.Hero.TieRate.ShouldBe(1, 1e-9);
        result.Hero.WinRate.ShouldBe(0, 1e-9);
    }

    [Fact]
    public async Task TheMonteCarloEstimateConvergesToTheExhaustiveResult()
    {
        EquityRequest spot = new()
        {
            PlayerRanges = [Range("AsKh"), RangeNotationParser.Parse("QQ+, AKs")],
            Board = TestCards.Parse("Qc7d2h"),
        };

        EquityResult exhaustive = await ComputeAsync(spot with { Method = EquityMethod.Exhaustive });
        EquityResult sampled = await ComputeAsync(spot with
        {
            Method = EquityMethod.MonteCarlo,
            RandomSeed = 99,
            MaximumSamples = 300_000,
            TargetStandardError = 0.0005,
        });

        exhaustive.WasExhaustive.ShouldBeTrue();
        sampled.WasExhaustive.ShouldBeFalse();
        sampled.Hero.Equity.ShouldBe(exhaustive.Hero.Equity, 0.005);
        sampled.HeroStandardError.ShouldBeLessThan(0.002);
    }

    [Fact]
    public async Task TheSameSeedAlwaysProducesTheSameEstimate()
    {
        EquityRequest spot = new()
        {
            PlayerRanges = [RangeNotationParser.Parse("77+"), RangeNotationParser.Parse("A2s+, KQo")],
            RandomSeed = 1234,
            MaximumSamples = 40_000,
            TargetStandardError = 0,
        };

        EquityResult first = await ComputeAsync(spot);
        EquityResult second = await ComputeAsync(spot);

        second.Hero.Equity.ShouldBe(first.Hero.Equity);
        second.SamplesEvaluated.ShouldBe(first.SamplesEvaluated);
    }

    [Fact]
    public async Task TheEquitiesOfAThreeWayPotAddUpToOne()
    {
        EquityResult result = await ComputeAsync(new EquityRequest
        {
            PlayerRanges = [Range("AsKs"), RangeNotationParser.Parse("QQ+"), RangeNotationParser.Parse("A2s-A5s")],
            Board = TestCards.Parse("Kh8d3c"),
            RandomSeed = 55,
            MaximumSamples = 80_000,
        });

        result.Players.Count.ShouldBe(3);
        result.Players.Sum(player => player.Equity).ShouldBe(1, 1e-9);
    }

    [Fact]
    public async Task ARangeEmptiedByTheBoardIsReported()
    {
        EquityException exception = await Should.ThrowAsync<EquityException>(() => ComputeAsync(new EquityRequest
        {
            PlayerRanges = [Range("AA"), HandRange.Full],
            Board = TestCards.Parse("AsAhAd7c"),
        }));

        exception.Message.ShouldContain("joueur 1");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public async Task AtLeastTwoPlayersAreRequired(int playerCount)
    {
        HandRange[] ranges = [.. Enumerable.Repeat(HandRange.Full, playerCount)];

        await Should.ThrowAsync<EquityException>(() => ComputeAsync(new EquityRequest { PlayerRanges = ranges }));
    }

    [Fact]
    public async Task ABoardOfAnImpossibleSizeIsRejected()
    {
        await Should.ThrowAsync<EquityException>(() => ComputeAsync(new EquityRequest
        {
            PlayerRanges = [HandRange.Full, HandRange.Full],
            Board = TestCards.Parse("AsKh"),
        }));
    }

    [Fact]
    public async Task ACardListedTwiceIsRejected()
    {
        await Should.ThrowAsync<EquityException>(() => ComputeAsync(new EquityRequest
        {
            PlayerRanges = [HandRange.Full, HandRange.Full],
            Board = TestCards.Parse("AsKh2c"),
            DeadCards = TestCards.Parse("As"),
        }));
    }

    [Fact]
    public async Task ACancelledRequestStopsInsteadOfReturningAResult()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => _calculator.ComputeAsync(
            new EquityRequest { PlayerRanges = [HandRange.Full, HandRange.Full] },
            cancellation.Token));
    }

    private Task<EquityResult> ComputeAsync(EquityRequest request)
    {
        return _calculator.ComputeAsync(request, TestContext.Current.CancellationToken);
    }

    private static HandRange Range(string notation)
    {
        return notation.Length == 4
            ? new HandRangeBuilder().Set(HoleCards.Parse(notation), 1).Build()
            : RangeNotationParser.Parse(notation);
    }
}
