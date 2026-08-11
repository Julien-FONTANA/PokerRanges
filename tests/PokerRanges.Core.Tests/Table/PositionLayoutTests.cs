using PokerRanges.Core.Table;
using Shouldly;

namespace PokerRanges.Core.Tests.Table;

public sealed class PositionLayoutTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void EveryTableSeatsExactlyItsNumberOfPlayersWithoutDuplicate(int playerCount)
    {
        IReadOnlyList<Position> seats = PositionLayout.Seats(playerCount);

        seats.Count.ShouldBe(playerCount);
        seats.Distinct().Count().ShouldBe(playerCount);
        seats.ShouldContain(Position.SmallBlind);
        seats.ShouldContain(Position.BigBlind);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void AnUnsupportedTableSizeIsRejected(int playerCount)
    {
        Should.Throw<TableException>(() => PositionLayout.Seats(playerCount));
    }

    [Fact]
    public void TheSixMaxTableUsesTheUsualNames()
    {
        PositionLayout.PreflopOrder(6).ShouldBe(
        [
            Position.UnderTheGun, Position.HiJack, Position.CutOff,
            Position.Button, Position.SmallBlind, Position.BigBlind,
        ]);
    }

    [Fact]
    public void TheFullTableRunsFromUnderTheGunToTheBigBlind()
    {
        PositionLayout.PreflopOrder(8).ShouldBe(
        [
            Position.UnderTheGun, Position.UnderTheGunPlusOne, Position.LoJack, Position.HiJack,
            Position.CutOff, Position.Button, Position.SmallBlind, Position.BigBlind,
        ]);
    }

    [Fact]
    public void HeadsUpTheSmallBlindOpensPreflopAndTheBigBlindOpensPostflop()
    {
        PositionLayout.PreflopOrder(2).ShouldBe([Position.SmallBlind, Position.BigBlind]);
        PositionLayout.PostflopOrder(2).ShouldBe([Position.BigBlind, Position.SmallBlind]);
    }

    [Fact]
    public void PostflopTheSmallBlindOpensAsSoonAsThereAreThreePlayers()
    {
        PositionLayout.PostflopOrder(6)[0].ShouldBe(Position.SmallBlind);
        PositionLayout.PostflopOrder(6)[^1].ShouldBe(Position.Button);
    }

    [Theory]
    [InlineData(8, Position.UnderTheGun, 7)]
    [InlineData(8, Position.Button, 2)]
    [InlineData(8, Position.SmallBlind, 1)]
    [InlineData(8, Position.BigBlind, 0)]
    [InlineData(6, Position.UnderTheGun, 5)]
    [InlineData(3, Position.Button, 2)]
    [InlineData(2, Position.SmallBlind, 1)]
    public void TheNumberOfPlayersLeftToActIsCountedFromThePreflopOrder(
        int playerCount,
        Position position,
        int expected)
    {
        PositionLayout.PlayersLeftToActPreflop(playerCount, position).ShouldBe(expected);
    }

    [Fact]
    public void AskingAboutAnAbsentPositionIsReported()
    {
        Should.Throw<TableException>(() => PositionLayout.PlayersLeftToActPreflop(4, Position.UnderTheGun));
        PositionLayout.IsSeated(4, Position.UnderTheGun).ShouldBeFalse();
        PositionLayout.IsSeated(4, Position.CutOff).ShouldBeTrue();
    }

    [Fact]
    public void TheButtonActsLastPostflop()
    {
        PositionLayout.ActsAfterPostflop(6, Position.Button, Position.SmallBlind).ShouldBeTrue();
        PositionLayout.ActsAfterPostflop(6, Position.SmallBlind, Position.Button).ShouldBeFalse();
    }
}
