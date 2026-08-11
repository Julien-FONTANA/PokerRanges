using PokerRanges.Core.Cards;
using PokerRanges.Core.Evaluation;
using Shouldly;

namespace PokerRanges.Core.Tests.Evaluation;

public sealed class BoardTextureTests
{
    [Fact]
    public void ADryRainbowBoardIsRecognisedAsSuch()
    {
        BoardTexture texture = BoardTexture.Read(TestCards.Parse("Kh7d2c"));

        texture.IsRainbow.ShouldBeTrue();
        texture.IsPaired.ShouldBeFalse();
        texture.AllowsFlushDraw.ShouldBeFalse();
        texture.AllowsStraight.ShouldBeFalse();
        texture.HighCard.ShouldBe(Rank.King);
        texture.Wetness.ShouldBeLessThan(0.2);
    }

    [Fact]
    public void AMonotoneBoardAllowsAFlushStraightAway()
    {
        BoardTexture texture = BoardTexture.Read(TestCards.Parse("Ks9s4s"));

        texture.IsMonotone.ShouldBeTrue();
        texture.AllowsFlush.ShouldBeTrue();
        texture.Wetness.ShouldBeGreaterThan(0.4);
    }

    [Fact]
    public void AConnectedTwoToneBoardIsTheWettestOfAll()
    {
        BoardTexture texture = BoardTexture.Read(TestCards.Parse("9s8s7d"));

        texture.IsTwoTone.ShouldBeTrue();
        texture.AllowsFlushDraw.ShouldBeTrue();
        texture.StraightWindows.ShouldBeGreaterThanOrEqualTo(3);
        texture.Wetness.ShouldBeGreaterThan(0.6);
    }

    [Fact]
    public void APairedBoardIsFlaggedAndSlightlyDrier()
    {
        BoardTexture paired = BoardTexture.Read(TestCards.Parse("9s9d4c"));
        BoardTexture trips = BoardTexture.Read(TestCards.Parse("9s9d9c"));

        paired.IsPaired.ShouldBeTrue();
        paired.HasTrips.ShouldBeFalse();
        trips.HasTrips.ShouldBeTrue();
    }

    [Fact]
    public void TheWheelCountsAsAStraightWindow()
    {
        BoardTexture texture = BoardTexture.Read(TestCards.Parse("As2d3c"));

        texture.AllowsStraight.ShouldBeTrue();
    }

    [Fact]
    public void TheDescriptionMentionsTheMainTraits()
    {
        BoardTexture.Read(TestCards.Parse("9s8s7d")).Describe().ShouldContain("connected");
        BoardTexture.Read(TestCards.Parse("Kh7d2c")).Describe().ShouldContain("dry");
    }

    [Theory]
    [InlineData("Kh7d")]
    [InlineData("Kh7d2c3s4h5d")]
    public void ABoardOfAnImpossibleSizeIsRejected(string cards)
    {
        Card[] board = TestCards.Parse(cards);

        Should.Throw<ArgumentException>(() => BoardTexture.Read(board));
    }
}
