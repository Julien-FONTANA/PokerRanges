using PokerRanges.Core.Cards;
using Shouldly;

namespace PokerRanges.Core.Tests.Cards;

/// <summary>
/// Reading is called again on every keystroke: it must therefore tell an entry in progress from a
/// faulty one, otherwise typing "a" before "s" would show an error on every card.
/// </summary>
public sealed class CardSequenceTests
{
    [Theory]
    [InlineData("askd")]
    [InlineData("AsKd")]
    [InlineData("ASKD")]
    [InlineData("as kd")]
    [InlineData("As,Kd")]
    [InlineData(" as-kd ")]
    public void TheSameTwoCardsAreReadWhateverTheSeparatorsAndTheCase(string text)
    {
        CardSequence sequence = CardSequence.Read(text, 2);

        sequence.HasError.ShouldBeFalse();
        sequence.Pending.ShouldBeEmpty();
        sequence.Cards.ShouldBe([Card.Parse("As"), Card.Parse("Kd")]);
    }

    [Fact]
    public void ARankWaitingForItsSuitIsASaisieInProgressAndNotAnError()
    {
        CardSequence sequence = CardSequence.Read("ask", 2);

        sequence.HasError.ShouldBeFalse();
        sequence.Cards.ShouldBe([Card.Parse("As")]);
        sequence.Pending.ShouldBe("K");
    }

    [Fact]
    public void AnEmptyTextIsAnEmptySelection()
    {
        CardSequence sequence = CardSequence.Read(string.Empty, 5);

        sequence.HasError.ShouldBeFalse();
        sequence.Cards.ShouldBeEmpty();
        sequence.Pending.ShouldBeEmpty();
    }

    [Fact]
    public void ACharacterThatIsNoRankIsRejectedByName()
    {
        CardSequence sequence = CardSequence.Read("xs", 2);

        sequence.HasError.ShouldBeTrue();
        sequence.Error!.ShouldContain("rank");
    }

    [Fact]
    public void ACharacterThatIsNoSuitIsRejectedByName()
    {
        CardSequence sequence = CardSequence.Read("ax", 2);

        sequence.HasError.ShouldBeTrue();
        sequence.Error!.ShouldContain("suit");
    }

    [Fact]
    public void TheSameCardTwiceIsRefusedBecauseThereIsOnlyOneDeck()
    {
        CardSequence sequence = CardSequence.Read("asas", 2);

        sequence.HasError.ShouldBeTrue();
        sequence.Error!.ShouldContain("As");
    }

    [Fact]
    public void ACardBeyondTheCapacityIsRefusedInsteadOfSilentlyDropped()
    {
        CardSequence sequence = CardSequence.Read("askdqh", 2);

        sequence.HasError.ShouldBeTrue();
        sequence.Error!.ShouldContain("Qh");
    }

    [Fact]
    public void AFullBoardIsReadInOneGo()
    {
        CardSequence sequence = CardSequence.Read("ks8d3c2h7s", 5);

        sequence.HasError.ShouldBeFalse();
        sequence.Cards.Count.ShouldBe(5);
    }

    [Fact]
    public void WhatIsWrittenIsReadBackIdentically()
    {
        IReadOnlyList<Card> cards = [Card.Parse("Ks"), Card.Parse("8d"), Card.Parse("3c")];

        string text = CardSequence.Write(cards);

        text.ShouldBe("Ks8d3c");
        CardSequence.Read(text, 5).Cards.ShouldBe(cards);
    }
}
