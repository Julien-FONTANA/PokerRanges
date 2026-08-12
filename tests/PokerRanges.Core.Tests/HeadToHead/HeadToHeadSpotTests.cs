using Microsoft.Extensions.Logging.Abstractions;
using PokerRanges.Core.HeadToHead;
using PokerRanges.Core.Table;
using Shouldly;

namespace PokerRanges.Core.Tests.HeadToHead;

public sealed class HeadToHeadSpotTests
{
    private readonly PotEngine _potEngine = new(NullLogger<PotEngine>.Instance);

    [Fact]
    public void AHeadsUpJamPutsTwiceTheEffectiveStackAtStake()
    {
        HeadToHeadSpot spot = Spot(HeadsUp(20, 20, Position.SmallBlind), HeadToHeadRole.Jamming);

        spot.EffectiveStack.ShouldBe(20, 1e-9);
        spot.DeadChips.ShouldBe(0, 1e-9);
        spot.ContestedPot.ShouldBe(40, 1e-9);

        // The small blind is already in, so only the 19 behind it is at risk.
        spot.HeroRisk.ShouldBe(19, 1e-9);

        // An uncalled jam comes straight back: a fold wins the blinds and nothing more.
        spot.UncontestedPot.ShouldBe(3, 1e-9);
    }

    /// <summary>
    /// The regression that matters: the raw pot holds the jammer's uncalled excess, and counting it
    /// would quote the caller a price that is far too good.
    /// </summary>
    [Fact]
    public void AShortCallerOnlyContestsTwiceTheirOwnStack()
    {
        HeadToHeadSpot spot = Spot(HeadsUp(20, 30, Position.BigBlind), HeadToHeadRole.CallingAJam);

        spot.EffectiveStack.ShouldBe(20, 1e-9);
        spot.ContestedPot.ShouldBe(40, 1e-9);
        spot.HeroRisk.ShouldBe(18, 1e-9);
        spot.BreakEvenEquityIfCalled.ShouldBe(0.45, 1e-9);
    }

    [Fact]
    public void ABigBlindAnteIsDeadMoneyRatherThanPartOfTheBet()
    {
        HeadToHeadSpot spot = Spot(
            HeadsUp(20, 20, Position.SmallBlind) with { AnteStyle = AnteStyle.BigBlindAnte, AnteAmount = 2 },
            HeadToHeadRole.Jamming);

        // The big blind posts 2 of ante plus the 2 blind; the small blind posts 1.
        spot.HeroCommitted.ShouldBe(1, 1e-9);
        spot.VillainCommitted.ShouldBe(4, 1e-9);
        spot.UncontestedPot.ShouldBe(5, 1e-9);

        // Nobody has folded, so none of it is dead — and the showdown is still two full stacks.
        spot.DeadChips.ShouldBe(0, 1e-9);
        spot.ContestedPot.ShouldBe(40, 1e-9);
        spot.HeroRisk.ShouldBe(19, 1e-9);
    }

    [Fact]
    public void TheBlindsAndAntesOfFoldedPlayersBecomeDeadMoney()
    {
        TableConfiguration table = TableConfiguration.Uniform(8, 2, 20, Position.Button) with
        {
            AnteStyle = AnteStyle.PerPlayer,
            AnteAmount = 1,
        };

        HeadToHeadSpot spot = HeadToHeadSpot.BetweenSeats(
            _potEngine,
            table,
            Position.BigBlind,
            HeadToHeadRole.Jamming);

        // Eight antes plus the blinds is 11; the button owns 1 of it and the big blind 3.
        spot.HeroCommitted.ShouldBe(1, 1e-9);
        spot.VillainCommitted.ShouldBe(3, 1e-9);
        spot.DeadChips.ShouldBe(7, 1e-9);

        spot.ContestedPot.ShouldBe(47, 1e-9);
        spot.HeroRisk.ShouldBe(19, 1e-9);
        spot.UncontestedPot.ShouldBe(11, 1e-9);
    }

    [Fact]
    public void ABigBlindShorterThanItsOwnAnteIsAlreadyAllIn()
    {
        HeadToHeadSpot spot = Spot(
            HeadsUp(2, 20, Position.BigBlind) with { AnteStyle = AnteStyle.BigBlindAnte, AnteAmount = 2 },
            HeadToHeadRole.CallingAJam);

        spot.EffectiveStack.ShouldBe(2, 1e-9);
        spot.HeroCommitted.ShouldBe(2, 1e-9);
        spot.HeroRisk.ShouldBe(0, 1e-9);
        spot.HeroIsAllIn.ShouldBeTrue();
    }

    [Fact]
    public void AnOpponentWithNothingBehindCannotFold()
    {
        HeadToHeadSpot spot = Spot(
            HeadsUp(20, 2, Position.SmallBlind) with { AnteStyle = AnteStyle.BigBlindAnte, AnteAmount = 2 },
            HeadToHeadRole.Jamming);

        spot.EffectiveStack.ShouldBe(2, 1e-9);
        spot.VillainRisk.ShouldBe(0, 1e-9);
        spot.VillainIsAllIn.ShouldBeTrue();
    }

    [Fact]
    public void TheDepthIsQuotedInBigBlinds()
    {
        HeadToHeadSpot spot = Spot(HeadsUp(24, 40, Position.SmallBlind), HeadToHeadRole.Jamming);

        spot.DepthInBigBlinds.ShouldBe(12, 1e-9);
    }

    [Fact]
    public void TheOpponentCannotBeTheHeroSeat()
    {
        Should.Throw<HeadToHeadException>(() => HeadToHeadSpot.BetweenSeats(
            _potEngine,
            HeadsUp(20, 20, Position.SmallBlind),
            Position.SmallBlind,
            HeadToHeadRole.Jamming));
    }

    [Fact]
    public void TheOpponentHasToBeSeatedAtThisTable()
    {
        Should.Throw<HeadToHeadException>(() => HeadToHeadSpot.BetweenSeats(
            _potEngine,
            HeadsUp(20, 20, Position.SmallBlind),
            Position.CutOff,
            HeadToHeadRole.Jamming));
    }

    private static TableConfiguration HeadsUp(double heroStack, double villainStack, Position heroSeat)
    {
        Position villainSeat = heroSeat == Position.SmallBlind ? Position.BigBlind : Position.SmallBlind;

        return new TableConfiguration
        {
            PlayerCount = 2,
            BigBlind = 2,
            StartingStacks = new Dictionary<Position, double>
            {
                [heroSeat] = heroStack,
                [villainSeat] = villainStack,
            },
            HeroPosition = heroSeat,
        };
    }

    private HeadToHeadSpot Spot(TableConfiguration table, HeadToHeadRole role)
    {
        Position villainSeat = table.HeroPosition == Position.SmallBlind
            ? Position.BigBlind
            : Position.SmallBlind;

        return HeadToHeadSpot.BetweenSeats(_potEngine, table, villainSeat, role);
    }
}
