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
/// La langue n'est pas une variable globale mais la culture d'interface du contexte courant. Ces
/// tests posent donc leur langue localement : elle ne déborde pas sur les tests voisins, et c'est
/// exactement ce qui permet au moteur de rendre ses phrases dans la bonne langue depuis un fil
/// d'arrière-plan.
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
    /// Un profil enregistré en français doit se retrouver après le passage à l'anglais : sinon le
    /// changement de langue remettrait discrètement l'utilisateur sur le profil par défaut.
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
    /// Changer de langue change aussi la culture de formatage : afficher « 5.5bb » dans une phrase
    /// française, ou « 5,5bb » dans une phrase anglaise, trahirait le bricolage.
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
    /// La langue voyage avec le contexte d'exécution : un calcul poussé sur un fil du pool doit
    /// rendre ses phrases dans la langue choisie par l'appelant, pas dans celle du système.
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
