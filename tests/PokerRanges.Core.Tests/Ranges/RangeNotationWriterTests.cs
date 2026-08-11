using PokerRanges.Core.Cards;
using PokerRanges.Core.Ranges;
using Shouldly;

namespace PokerRanges.Core.Tests.Ranges;

public sealed class RangeNotationWriterTests
{
    [Theory]
    [InlineData("AA", "AA")]
    [InlineData("77+", "77+")]
    [InlineData("55-99", "55-99")]
    [InlineData("ATs+", "ATs+")]
    [InlineData("A2s-A5s", "A2s-A5s")]
    [InlineData("AKo:0.5", "AKo:0.5")]
    [InlineData("77+, ATs+, KQo", "77+, ATs+, KQo")]
    public void TheCompactFormIsRestoredWhenWritingBack(string notation, string expected)
    {
        RangeNotationWriter.Write(RangeNotationParser.Parse(notation)).ShouldBe(expected);
    }

    [Fact]
    public void AnEmptyRangeIsWrittenAsAnEmptyString()
    {
        RangeNotationWriter.Write(HandRange.Empty).ShouldBeEmpty();
    }

    [Fact]
    public void AClassWithUnevenComboWeightsFallsBackToListingItsCombos()
    {
        HandRangeBuilder builder = new();
        builder.Set(HoleCards.Parse("AsKs"), 1);
        builder.Set(HoleCards.Parse("AhKh"), 0.5);

        string notation = RangeNotationWriter.Write(builder.Build());

        notation.ShouldContain("AsKs");
        notation.ShouldContain("AhKh:0.5");
    }

    [Fact]
    public void EveryUniformlyWeightedRangeSurvivesAWriteThenParseRoundTrip()
    {
        Random random = new(4242);
        double[] weights = [0, 0, 0.25, 0.5, 0.75, 1];

        for (int iteration = 0; iteration < 200; iteration++)
        {
            HandRangeBuilder builder = new();
            foreach (HandClass handClass in HandClass.All)
            {
                builder.Set(handClass, weights[random.Next(weights.Length)]);
            }

            HandRange original = builder.Build();
            HandRange restored = RangeNotationParser.Parse(RangeNotationWriter.Write(original));

            restored.ToWeightArray().ShouldBe(original.ToWeightArray());
        }
    }

    [Fact]
    public void ARangeWeightedComboByComboSurvivesAWriteThenParseRoundTrip()
    {
        Random random = new(1337);
        HandRangeBuilder builder = new();

        foreach (HoleCards combo in HoleCards.All())
        {
            builder.Set(combo, random.Next(5) switch { 0 => 0, 1 => 0.25, 2 => 0.5, 3 => 0.75, _ => 1 });
        }

        HandRange original = builder.Build();
        HandRange restored = RangeNotationParser.Parse(RangeNotationWriter.Write(original));

        restored.ToWeightArray().ShouldBe(original.ToWeightArray());
    }
}
