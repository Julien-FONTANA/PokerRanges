using PokerRanges.Core.Cards;
using PokerRanges.Core.Ranges;
using Shouldly;

namespace PokerRanges.Core.Tests.Ranges;

public sealed class RangeNotationParserTests
{
    [Theory]
    [InlineData("AA", 6)]
    [InlineData("AKs", 4)]
    [InlineData("AKo", 12)]
    [InlineData("AK", 16)]
    [InlineData("AsKh", 1)]
    [InlineData("77+", 48)]
    [InlineData("22+", 78)]
    [InlineData("ATs+", 16)]
    [InlineData("A5s-A2s", 16)]
    [InlineData("A2s-A5s", 16)]
    [InlineData("98s-65s", 16)]
    [InlineData("55-99", 30)]
    [InlineData("QJs+", 4)]
    [InlineData("77+, ATs+, KQo", 76)]
    public void TheNotationExpandsToTheExpectedNumberOfCombos(string notation, int expectedCombos)
    {
        RangeNotationParser.Parse(notation).TotalCombos.ShouldBe(expectedCombos, 1e-9);
    }

    [Fact]
    public void AnOpenEndedPairTokenCoversEveryPairAboveIt()
    {
        HandRange range = RangeNotationParser.Parse("TT+");

        range.FrequencyOf(HandClass.Parse("TT")).ShouldBe(1);
        range.FrequencyOf(HandClass.Parse("AA")).ShouldBe(1);
        range.FrequencyOf(HandClass.Parse("99")).ShouldBe(0);
    }

    [Fact]
    public void AnOpenEndedSuitedTokenWalksTheLowCardUpToTheHighCard()
    {
        HandRange range = RangeNotationParser.Parse("KTs+");

        range.FrequencyOf(HandClass.Parse("KTs")).ShouldBe(1);
        range.FrequencyOf(HandClass.Parse("KQs")).ShouldBe(1);
        range.FrequencyOf(HandClass.Parse("K9s")).ShouldBe(0);
        range.FrequencyOf(HandClass.Parse("KTo")).ShouldBe(0);
        range.FrequencyOf(HandClass.Parse("AKs")).ShouldBe(0);
    }

    /// <summary>
    /// Le « + » fait monter le kicker en gardant la carte haute, comme PokerStove, Equilab et GTO+ :
    /// « QJs+ » ne vaut que QJs. Pour la lecture « connecteurs assortis à partir de QJs », la
    /// notation à borner est la bonne : « AKs-QJs ».
    /// </summary>
    [Fact]
    public void TheOpenEndedFormMovesTheKickerAndNotBothCards()
    {
        HandRange kicker = RangeNotationParser.Parse("QJs+");
        HandRange connectors = RangeNotationParser.Parse("AKs-QJs");

        kicker.TotalCombos.ShouldBe(4, 1e-9);
        kicker.FrequencyOf(HandClass.Parse("KQs")).ShouldBe(0);

        connectors.TotalCombos.ShouldBe(12, 1e-9);
        connectors.FrequencyOf(HandClass.Parse("KQs")).ShouldBe(1);
        connectors.FrequencyOf(HandClass.Parse("AKs")).ShouldBe(1);
    }

    [Fact]
    public void AConnectorRangeWalksBothCardsDownTogether()
    {
        HandRange range = RangeNotationParser.Parse("98s-65s");

        foreach (string hand in new[] { "98s", "87s", "76s", "65s" })
        {
            range.FrequencyOf(HandClass.Parse(hand)).ShouldBe(1, $"{hand} devrait être inclus");
        }

        range.FrequencyOf(HandClass.Parse("T9s")).ShouldBe(0);
        range.FrequencyOf(HandClass.Parse("54s")).ShouldBe(0);
        range.FrequencyOf(HandClass.Parse("97s")).ShouldBe(0);
    }

    [Fact]
    public void AWeightAppliesToEveryComboOfTheToken()
    {
        HandRange range = RangeNotationParser.Parse("AKo:0.5");

        range.FrequencyOf(HandClass.Parse("AKo")).ShouldBe(0.5, 1e-9);
        range.TotalCombos.ShouldBe(6, 1e-9);
    }

    [Fact]
    public void ATokenListedTwiceKeepsTheWeightOfItsLastMention()
    {
        HandRange range = RangeNotationParser.Parse("AA, AA:0.25");

        range.FrequencyOf(HandClass.Parse("AA")).ShouldBe(0.25, 1e-9);
    }

    [Fact]
    public void SeparatorsAndCasingAreTolerated()
    {
        HandRange withCommas = RangeNotationParser.Parse("77+, ATs+, KQo");
        HandRange withSpaces = RangeNotationParser.Parse("77+  ats+  kqo");

        withSpaces.ToWeightArray().ShouldBe(withCommas.ToWeightArray());
    }

    [Theory]
    [InlineData("AAs")]
    [InlineData("AKz")]
    [InlineData("AKs-QQ")]
    [InlineData("A5s-K2s")]
    [InlineData("AA:1.5")]
    [InlineData("AA:abc")]
    [InlineData("XX")]
    public void MalformedNotationIsRejectedWithTheOffendingToken(string notation)
    {
        RangeNotationException exception = Should.Throw<RangeNotationException>(
            () => RangeNotationParser.Parse(notation));

        exception.Token.ShouldBe(notation.Trim());
    }

    [Fact]
    public void TryParseReportsFailureWithoutThrowing()
    {
        RangeNotationParser.TryParse("AAs", out HandRange range).ShouldBeFalse();
        range.IsEmpty.ShouldBeTrue();

        RangeNotationParser.TryParse("AA", out HandRange valid).ShouldBeTrue();
        valid.TotalCombos.ShouldBe(6, 1e-9);
    }
}
