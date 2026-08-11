using PokerRanges.Core.Cards;
using PokerRanges.Core.Ranges;
using Shouldly;

namespace PokerRanges.Core.Tests.Ranges;

public sealed class HandRangeTests
{
    [Fact]
    public void TheFullRangeHoldsEveryComboAndTheEmptyRangeNone()
    {
        HandRange.Full.TotalCombos.ShouldBe(1326, 1e-9);
        HandRange.Full.PercentOfAllHands.ShouldBe(100, 1e-9);
        HandRange.Empty.IsEmpty.ShouldBeTrue();
        HandRange.Empty.TotalCombos.ShouldBe(0, 1e-9);
    }

    [Fact]
    public void RemovingABoardCardDropsEveryComboThatUsesIt()
    {
        HandRange aces = RangeNotationParser.Parse("AA");

        HandRange withoutAceOfSpades = aces.WithoutCards(TestCards.Parse("As"));

        withoutAceOfSpades.TotalCombos.ShouldBe(3, 1e-9);
        withoutAceOfSpades.GetWeight(HoleCards.Parse("AhAd")).ShouldBe(1);
        withoutAceOfSpades.GetWeight(HoleCards.Parse("AsAh")).ShouldBe(0);
    }

    [Fact]
    public void AFiveCardBoardLeavesTheCombosOfTheFortySevenRemainingCards()
    {
        HandRange remaining = HandRange.Full.WithoutCards(TestCards.Parse("AsKhQd7c2s"));

        remaining.TotalCombos.ShouldBe(47 * 46 / 2, 1e-9);
    }

    [Fact]
    public void TheFrequencyOfAClassIsItsShareOfCombosInTheRange()
    {
        HandRangeBuilder builder = new();
        builder.Set(HoleCards.Parse("AsKs"), 1);
        builder.Set(HoleCards.Parse("AhKh"), 1);

        HandRange range = builder.Build();

        range.WeightOf(HandClass.Parse("AKs")).ShouldBe(2, 1e-9);
        range.FrequencyOf(HandClass.Parse("AKs")).ShouldBe(0.5, 1e-9);
    }

    [Fact]
    public void ScalingMultipliesEveryWeightAndStaysCappedAtOne()
    {
        HandRange halved = RangeNotationParser.Parse("AA").Scaled(0.5);
        HandRange doubled = RangeNotationParser.Parse("AA:0.25").Scaled(8);

        halved.TotalCombos.ShouldBe(3, 1e-9);
        doubled.FrequencyOf(HandClass.Parse("AA")).ShouldBe(1, 1e-9);
    }

    [Fact]
    public void EnumeratingSkipsTheCombosThatAreNotInTheRange()
    {
        HandRange range = RangeNotationParser.Parse("AKs");

        WeightedCombo[] combos = [.. range.EnumerateCombos()];

        combos.Length.ShouldBe(4);
        combos.ShouldAllBe(entry => entry.Weight == 1);
        combos.Select(entry => entry.Combo.ToHandClass()).Distinct().Single().ShouldBe(HandClass.Parse("AKs"));
    }
}
