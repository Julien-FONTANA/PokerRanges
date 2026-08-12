using Microsoft.Extensions.Logging.Abstractions;
using PokerRanges.App.ViewModels;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Equity;
using PokerRanges.Core.Evaluation;
using PokerRanges.Core.HeadToHead;
using PokerRanges.Core.Ranges;
using PokerRanges.Core.Table;
using Shouldly;

namespace PokerRanges.App.Tests;

/// <remarks>
/// Nothing here touches <see cref="PokerRanges.Core.Localization.Language"/>: it is process-global
/// static state, and toggling it would flake every text assertion in the other test class. Language
/// coverage lives there, where it is toggled back.
/// </remarks>
[Collection("view models")]
public sealed class HeadToHeadViewModelTests
{
    [Fact]
    public void TypingARangeFillsTheGrid()
    {
        HeadToHeadViewModel viewModel = Build();

        viewModel.HeroRange.Notation = "AA, KK";

        viewModel.HeroRange.Range.TotalCombos.ShouldBe(12, 1e-9);
        Cell(viewModel.HeroRange, "AA").IsPicked.ShouldBeTrue();
        Cell(viewModel.HeroRange, "KK").IsPicked.ShouldBeTrue();
        Cell(viewModel.HeroRange, "QQ").IsPicked.ShouldBeFalse();
    }

    [Fact]
    public void ClickingACellRewritesTheNotation()
    {
        HeadToHeadViewModel viewModel = Build();
        viewModel.HeroRange.Notation = string.Empty;

        viewModel.HeroRange.Toggle(Cell(viewModel.HeroRange, "AA"));

        viewModel.HeroRange.Notation.ShouldBe("AA");
        viewModel.HeroRange.Range.TotalCombos.ShouldBe(6, 1e-9);

        viewModel.HeroRange.Toggle(Cell(viewModel.HeroRange, "AA"));

        viewModel.HeroRange.Range.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void AHalfTypedTokenLeavesTheRangeAlone()
    {
        HeadToHeadViewModel viewModel = Build();
        viewModel.HeroRange.Notation = "AA";

        viewModel.HeroRange.Notation = "AA, Z";

        viewModel.HeroRange.HasNotationError.ShouldBeTrue();
        viewModel.HeroRange.Range.TotalCombos.ShouldBe(6, 1e-9);
    }

    [Fact]
    public void TheSliderTakesTheStrongestHandsFirst()
    {
        HeadToHeadViewModel viewModel = Build();

        viewModel.VillainRange.TopPercent = 10;

        viewModel.VillainRange.Range.TotalCombos.ShouldBe(132.6, 1e-9);
        viewModel.VillainRange.Range.FrequencyOf(HandClass.Parse("AA")).ShouldBe(1, 1e-9);
        viewModel.VillainRange.Range.FrequencyOf(HandClass.Parse("72o")).ShouldBe(0, 1e-9);
    }

    /// <summary>
    /// The slider writes into the range and is never written back from it. Were it recomputed, one
    /// click would snap it and re-derive a whole percentile over the top of the edit.
    /// </summary>
    [Fact]
    public void ClickingACellDoesNotMoveTheSlider()
    {
        HeadToHeadViewModel viewModel = Build();
        viewModel.HeroRange.TopPercent = 20;

        viewModel.HeroRange.Toggle(Cell(viewModel.HeroRange, "72o"));

        viewModel.HeroRange.TopPercent.ShouldBe(20);
        viewModel.HeroRange.Range.FrequencyOf(HandClass.Parse("72o")).ShouldBe(1, 1e-9);
        viewModel.HeroRange.Range.FrequencyOf(HandClass.Parse("AA")).ShouldBe(1, 1e-9);
    }

    [Fact]
    public void SwappingSidesSwapsTheRangesAndWhoIsJamming()
    {
        HeadToHeadViewModel viewModel = Build();
        viewModel.HeroRange.Notation = "AA";
        viewModel.VillainRange.Notation = "KK";
        viewModel.Role = viewModel.Roles.Single(role => role.Value == HeadToHeadRole.Jamming);

        viewModel.SwapSides();

        viewModel.HeroRange.Notation.ShouldBe("KK");
        viewModel.VillainRange.Notation.ShouldBe("AA");
        viewModel.Role.Value.ShouldBe(HeadToHeadRole.CallingAJam);
    }

    [Fact]
    public void TheActiveSideSelectorSwitchesWhichRangeTheGridEdits()
    {
        HeadToHeadViewModel viewModel = Build();

        viewModel.EditHero();
        viewModel.ActiveRange.ShouldBeSameAs(viewModel.HeroRange);
        viewModel.IsEditingHero.ShouldBeTrue();

        viewModel.EditVillain();
        viewModel.ActiveRange.ShouldBeSameAs(viewModel.VillainRange);
        viewModel.IsEditingHero.ShouldBeFalse();
    }

    [Fact]
    public async Task AJamAtTwelveBigBlindsIsMeasuredAgainstTheCallingRange()
    {
        HeadToHeadViewModel viewModel = Build();
        viewModel.HeroIsExactHand = false;
        viewModel.HeroRange.Notation = "AA";
        viewModel.VillainIsExactHand = false;
        viewModel.VillainRange.Notation = "22+, A2s+, A2o+";

        await viewModel.ComputeNowAsync();

        viewModel.Result.HasProblem.ShouldBeFalse();
        viewModel.Result.Actions.Count.ShouldBe(2);
        viewModel.Result.Actions[0].Label.ShouldStartWith("Jam");
        viewModel.Result.Equity.ShouldNotBeEmpty();
        viewModel.Result.HasPrecision.ShouldBeTrue();
        viewModel.DepthLabel.ShouldBe("12bb");
    }

    [Fact]
    public async Task FacingAJamTheCallIsComparedWithFolding()
    {
        HeadToHeadViewModel viewModel = Build();
        viewModel.Role = viewModel.Roles.Single(role => role.Value == HeadToHeadRole.CallingAJam);
        viewModel.HeroIsExactHand = false;
        viewModel.HeroRange.Notation = "AA";
        viewModel.VillainIsExactHand = false;
        viewModel.VillainRange.Notation = "22+, A2s+, A2o+";

        await viewModel.ComputeNowAsync();

        viewModel.Result.HasProblem.ShouldBeFalse();
        viewModel.Result.Actions[0].Label.ShouldStartWith("Call");

        // A jamming range is not an acceptance set, so no fold frequency is quoted against it.
        viewModel.Result.BreakEvenFold.ShouldBe("not needed");
    }

    [Fact]
    public async Task AnEmptySideIsReportedRatherThanComputed()
    {
        HeadToHeadViewModel viewModel = Build();
        viewModel.HeroIsExactHand = false;
        viewModel.HeroRange.Notation = string.Empty;

        await viewModel.ComputeNowAsync();

        viewModel.Result.HasProblem.ShouldBeTrue();
    }

    [Fact]
    public async Task AHalfTypedBoardIsReportedRatherThanComputed()
    {
        HeadToHeadViewModel viewModel = Build();
        viewModel.HeroIsExactHand = false;
        viewModel.VillainIsExactHand = false;
        viewModel.Board.QuickEntry = "Ks8d";

        await viewModel.ComputeNowAsync();

        viewModel.Board.Selection.Count.ShouldBe(2);
        viewModel.Result.HasProblem.ShouldBeTrue();
    }

    [Fact]
    public void ACardTakenByTheBoardIsNoLongerOfferedToEitherHand()
    {
        HeadToHeadViewModel viewModel = Build();

        viewModel.Board.QuickEntry = "Ks8d3c";

        viewModel.HeroCards.Cards.Single(option => option.Card == Card.Parse("Ks")).IsAvailable.ShouldBeFalse();
        viewModel.VillainCards.Cards.Single(option => option.Card == Card.Parse("Ks")).IsAvailable.ShouldBeFalse();
        viewModel.HeroCards.Cards.Single(option => option.Card == Card.Parse("Ah")).IsAvailable.ShouldBeTrue();
    }

    private static RangeGridCellViewModel Cell(RangeGridViewModel grid, string notation)
    {
        HandClass handClass = HandClass.Parse(notation);

        return grid.Cells.Single(cell => cell.HandClass == handClass);
    }

    private static HeadToHeadViewModel Build()
    {
        RankCountHandEvaluator evaluator = new();

        return new HeadToHeadViewModel(
            new PotEngine(NullLogger<PotEngine>.Instance),
            new HeadToHeadCoordinator(
                new HeadToHeadCalculator(
                    new EquityCalculator(evaluator, NullLogger<EquityCalculator>.Instance),
                    NullLogger<HeadToHeadCalculator>.Instance),
                NullLogger<HeadToHeadCoordinator>.Instance),
            new PreflopHandStrength(),
            NullLogger<HeadToHeadViewModel>.Instance);
    }
}
