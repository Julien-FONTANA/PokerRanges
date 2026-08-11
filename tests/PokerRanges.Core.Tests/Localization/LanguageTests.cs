using System.Globalization;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Evaluation;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Postflop;
using PokerRanges.Core.Preflop;
using PokerRanges.Core.Table;
using Shouldly;

namespace PokerRanges.Core.Tests.Localization;

/// <summary>
/// The language is not a global variable but the UI culture of the current context. These tests
/// therefore set their language locally: it does not spill onto neighbouring tests, and that is
/// exactly what lets the engine render its sentences in the right language from a background
/// thread.
/// </summary>
public sealed class LanguageTests
{
    [Fact]
    public void EnglishIsWhatTheEngineSpeaksByDefault()
    {
        Language.Current.ShouldBe(AppLanguage.English);
        Language.IsFrench.ShouldBeFalse();
    }

    [Fact]
    public void TheSameHandIsNamedInBothLanguages()
    {
        RankCountHandEvaluator evaluator = new();
        HandValue value = evaluator.Evaluate(TestCards.Parse("AsKsQsJsTs"));

        value.Describe().ShouldBe("Royal flush");

        using (Speaking(AppLanguage.French))
        {
            value.Describe().ShouldBe("Quinte flush royale");
        }

        value.Describe().ShouldBe("Royal flush");
    }

    [Fact]
    public void TheBoardTextureFollowsTheLanguage()
    {
        BoardTexture texture = BoardTexture.Read(TestCards.Parse("9s8s7d"));

        texture.Describe().ShouldContain("two-tone");

        using (Speaking(AppLanguage.French))
        {
            texture.Describe().ShouldContain("bicolore");
        }
    }

    [Fact]
    public void ThePreflopVocabularyFollowsTheLanguage()
    {
        PreflopContextLabels.Describe(PreflopContext.VersusOpen).ShouldBe("Facing an open");
        new StrategyOption(ChartActionKind.Jam, 0, 1).Describe().ShouldBe("Jam");

        using (Speaking(AppLanguage.French))
        {
            PreflopContextLabels.Describe(PreflopContext.VersusOpen).ShouldBe("Face à une ouverture");
            new StrategyOption(ChartActionKind.Jam, 0, 1).Describe().ShouldBe("Tapis");
        }
    }

    [Fact]
    public void TheOpponentProfileNameFollowsTheLanguageWithoutLosingItsIdentity()
    {
        OpponentProfile profile = OpponentProfile.CallingStation;

        profile.Name.ShouldBe("Calling station");

        using (Speaking(AppLanguage.French))
        {
            profile.Name.ShouldBe("Suiveur");
        }

        profile.ShouldBeSameAs(OpponentProfile.CallingStation);
    }

    /// <summary>
    /// A profile saved in French must still be found after switching to English: otherwise
    /// changing language would quietly put the user back on the default profile.
    /// </summary>
    [Theory]
    [InlineData("Suiveur")]
    [InlineData("Calling station")]
    [InlineData("calling station")]
    public void AProfileSavedInEitherLanguageIsFoundAgain(string name)
    {
        OpponentProfile.Find(name).ShouldBeSameAs(OpponentProfile.CallingStation);
    }

    [Fact]
    public void AnUnknownProfileNameIsReportedRatherThanGuessed()
    {
        OpponentProfile.Find("Maniaque").ShouldBeNull();
    }

    /// <summary>
    /// Changing language also changes the formatting culture: showing "5.5bb" in a French
    /// sentence, or "5,5bb" in an English one, would give the patch-up away.
    /// </summary>
    [Fact]
    public void NumbersAreWrittenTheWayTheLanguageWritesThem()
    {
        new StrategyOption(ChartActionKind.Raise, 2.5, 1).Describe().ShouldBe("Raise to 2.5bb");

        using (Speaking(AppLanguage.French))
        {
            new StrategyOption(ChartActionKind.Raise, 2.5, 1).Describe().ShouldBe("Relancer à 2,5bb");
        }
    }

    [Fact]
    public void AnErrorTheUserCanTriggerIsTranslatedToo()
    {
        CardSequence.Read("ax", 2).Error!.ShouldContain("suit");

        using (Speaking(AppLanguage.French))
        {
            CardSequence.Read("ax", 2).Error!.ShouldContain("couleur");
        }
    }

    /// <summary>
    /// The language travels with the execution context: a computation pushed onto a pool thread
    /// must render its sentences in the language the caller chose, not the system's.
    /// </summary>
    [Fact]
    public async Task TheLanguageFollowsTheWorkOntoABackgroundThread()
    {
        using (Speaking(AppLanguage.French))
        {
            string described = await Task.Run(() => BoardTexture.Read(TestCards.Parse("9s8s7d")).Describe());

            described.ShouldContain("bicolore");
        }
    }

    private static LanguageScope Speaking(AppLanguage language)
    {
        return new LanguageScope(language);
    }
}
