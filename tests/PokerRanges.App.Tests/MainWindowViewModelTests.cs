using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using PokerRanges.App.ViewModels;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Equity;
using PokerRanges.Core.Evaluation;
using PokerRanges.Core.Postflop;
using PokerRanges.Core.Preflop;
using PokerRanges.Core.Session;
using PokerRanges.Core.Table;
using PokerRanges.Data;
using PokerRanges.Data.Storage;
using Shouldly;

namespace PokerRanges.App.Tests;

/// <summary>
/// Vérifie le câblage complet de l'interface : saisir une table, une main, un board et des actions
/// produit bien une recommandation issue du moteur, sans passer par un rendu graphique.
/// </summary>
public sealed class MainWindowViewModelTests : IDisposable
{
    private readonly List<string> _directories = [];

    public void Dispose()
    {
        foreach (string directory in _directories)
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task TheGridIsFilledAsSoonAsTheTableIsKnown()
    {
        MainWindowViewModel viewModel = await BuildAsync();

        viewModel.Matrix.Cells.Count.ShouldBe(169);
        viewModel.Matrix.Title.ShouldContain("Open-raise");
        viewModel.Recommendation.HasProblem.ShouldBeTrue();
        viewModel.Recommendation.Problem!.ShouldContain("two cards");
    }

    [Fact]
    public async Task ChoosingTwoCardsProducesARecommendation()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await SelectAsync(viewModel, viewModel.Hero, "Ah", "Qd");

        viewModel.Hero.AsHoleCards.ShouldBe(HoleCards.Parse("AhQd"));
        viewModel.Recommendation.Headline.ShouldStartWith("Raise");
        viewModel.Recommendation.Rationale.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task TheHeroHandIsHighlightedInTheGrid()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await SelectAsync(viewModel, viewModel.Hero, "Ah", "Qd");

        viewModel.Matrix.Cells.Single(cell => cell.IsHeroHand).Label.ShouldBe("AQo");
    }

    [Fact]
    public async Task AShortStackedButtonIsToldToJam()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        viewModel.Table.PlayerCount = 3;
        viewModel.Table.StartingStack = 96;
        viewModel.Table.HeroPosition = Choice(viewModel, Position.Button);
        await SelectAsync(viewModel, viewModel.Hero, "As", "2s");

        viewModel.Recommendation.Headline.ShouldBe("Jam");
        viewModel.Matrix.Title.ShouldContain("12bb");
    }

    [Fact]
    public async Task RecordingActionsAdvancesTheTurnAndFillsTheHistory()
    {
        MainWindowViewModel viewModel = await BuildAsync();

        viewModel.TurnLabel.ShouldContain("UTG");
        viewModel.CanFold.ShouldBeTrue();

        viewModel.Fold();
        await viewModel.RefreshNowAsync();

        viewModel.History.Single().Label.ShouldBe("UTG folds");
        viewModel.TurnLabel.ShouldContain("UTG+1");
        viewModel.CanUndo.ShouldBeTrue();
    }

    [Fact]
    public async Task ChangingTheTableClearsTheHandInProgress()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        viewModel.Fold();
        await viewModel.RefreshNowAsync();

        viewModel.Table.PlayerCount = 6;
        await viewModel.RefreshNowAsync();

        viewModel.History.ShouldBeEmpty();
        viewModel.CanUndo.ShouldBeFalse();
    }

    [Fact]
    public async Task ACardTakenByTheHeroCannotBePutOnTheBoard()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await SelectAsync(viewModel, viewModel.Hero, "Ah", "Qd");

        Option(viewModel.Board, "Ah").IsAvailable.ShouldBeFalse();
        Option(viewModel.Board, "Kh").IsAvailable.ShouldBeTrue();
    }

    [Fact]
    public async Task AnIncompleteFlopIsReportedInsteadOfCrashing()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await SelectAsync(viewModel, viewModel.Board, "Kh", "8d");

        viewModel.Recommendation.HasProblem.ShouldBeTrue();
        viewModel.Recommendation.Problem!.ShouldContain("Incomplete board");
    }

    [Fact]
    public async Task AFlopSwitchesTheAdviceToThePostflopEngine()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await ReachTheFlopAsync(viewModel, "Kh9d", "Ks8d3c");

        viewModel.StreetLabel.ShouldBe("Flop");
        viewModel.Recommendation.HasEvaluations.ShouldBeTrue();
        viewModel.Recommendation.Evaluations.Count(row => row.IsBest).ShouldBe(1);
        viewModel.Recommendation.Rationale.ShouldContain(line => line.Contains("high board", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheGridShowsTheOpponentAssignedRangeOnceTheFlopIsOut()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await ReachTheFlopAsync(viewModel, "Kh9d", "Ks8d3c");

        viewModel.Matrix.Title.ShouldContain("Range assigned");
        viewModel.Matrix.Title.ShouldContain("BB");
        viewModel.Matrix.Cells.Count.ShouldBe(169);
    }

    [Fact]
    public async Task ChangingTheOpponentProfileChangesTheAdvice()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await ReachTheFlopAsync(viewModel, "Kh9d", "Ks8d3c");

        string balanced = string.Join("|", viewModel.Recommendation.Evaluations.Select(row => row.ExpectedValue));

        viewModel.Profile = OpponentProfileChoice.All.Single(choice => choice.Value == OpponentProfile.CallingStation);
        await viewModel.RefreshNowAsync();

        string station = string.Join("|", viewModel.Recommendation.Evaluations.Select(row => row.ExpectedValue));

        station.ShouldNotBe(balanced);
    }

    [Fact]
    public async Task FoldingAsTheHeroStopsTheAdvice()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        viewModel.Table.HeroPosition = Choice(viewModel, Position.UnderTheGun);
        await SelectAsync(viewModel, viewModel.Hero, "7h", "2c");

        viewModel.Fold();
        await viewModel.RefreshNowAsync();

        viewModel.Recommendation.HasProblem.ShouldBeTrue();
        viewModel.Recommendation.Problem!.ShouldContain("folded");
    }

    [Fact]
    public async Task TypingTheCardsSelectsThemWithoutTouchingTheGrid()
    {
        MainWindowViewModel viewModel = await BuildAsync();

        viewModel.Hero.QuickEntry = "askd";
        await viewModel.RefreshNowAsync();

        viewModel.Hero.AsHoleCards.ShouldBe(HoleCards.Parse("AsKd"));
        viewModel.Hero.HasEntryError.ShouldBeFalse();
        viewModel.Matrix.Cells.Single(cell => cell.IsHeroHand).Label.ShouldBe("AKo");
    }

    /// <summary>
    /// Le point délicat de la saisie continue : après « a », il ne s'est encore rien passé, mais
    /// réécrire le champ effacerait la lettre à peine tapée.
    /// </summary>
    [Fact]
    public async Task AHalfTypedCardLeavesTheTextAloneAndRaisesNoError()
    {
        MainWindowViewModel viewModel = await BuildAsync();

        viewModel.Hero.QuickEntry = "a";
        await viewModel.RefreshNowAsync();

        viewModel.Hero.QuickEntry.ShouldBe("a");
        viewModel.Hero.HasEntryError.ShouldBeFalse();
        viewModel.Hero.Selection.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClickingACardWritesItBackIntoTheKeyboardEntry()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await SelectAsync(viewModel, viewModel.Hero, "Ah", "Qd");

        viewModel.Hero.QuickEntry.ShouldBe("AhQd");
    }

    [Fact]
    public async Task AMistypedCardIsReportedAndChangesNothing()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await SelectAsync(viewModel, viewModel.Hero, "Ah", "Qd");

        viewModel.Hero.QuickEntry = "azqd";

        viewModel.Hero.HasEntryError.ShouldBeTrue();
        viewModel.Hero.EntryError!.ShouldContain("suit");
        viewModel.Hero.AsHoleCards.ShouldBe(HoleCards.Parse("AhQd"));
    }

    [Fact]
    public async Task ACardAlreadyOnTheBoardCannotBeTypedIntoTheHand()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        viewModel.Board.QuickEntry = "ks8d3c";
        await viewModel.RefreshNowAsync();

        viewModel.Hero.QuickEntry = "ks9d";

        viewModel.Hero.HasEntryError.ShouldBeTrue();
        viewModel.Hero.EntryError!.ShouldContain("Ks");
    }

    [Fact]
    public async Task TheCompactModeSwitchesToTheReducedBudgetAndSaysWhatItCost()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await ReachTheFlopAsync(viewModel, "Kh9d", "Ks8d3c");

        viewModel.Budget.ShouldBe(PostflopBudget.Full);

        viewModel.ToggleCompact();
        await viewModel.RefreshNowAsync();

        viewModel.IsCompact.ShouldBeTrue();
        viewModel.Budget.ShouldBe(PostflopBudget.Fast);
        viewModel.Recommendation.HasPrecision.ShouldBeTrue();
        viewModel.Recommendation.Precision!.ShouldContain("Fast");
    }

    [Fact]
    public async Task TheCompactModeKeepsTheTableInSight()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await ReachTheFlopAsync(viewModel, "Kh9d", "Ks8d3c");

        viewModel.HandSummary.ShouldContain("6 players");
        viewModel.HandSummary.ShouldContain("BTN");
        viewModel.HandSummary.ShouldContain("100bb");
        viewModel.HandSummary.ShouldContain("K♠");
    }

    /// <summary>
    /// Le critère de la phase : une main entière se saisit au clavier — les cartes au texte, les
    /// actions aux raccourcis — sans jamais viser une case de la grille des 52 cartes.
    /// </summary>
    [Fact]
    public async Task AWholeHandIsPlayableFromTheKeyboardAlone()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        viewModel.Table.PlayerCount = 6;
        viewModel.Table.StartingStack = 800;
        viewModel.Table.HeroPosition = Choice(viewModel, Position.Button);

        viewModel.Hero.QuickEntry = "kh9d";
        viewModel.Fold();
        viewModel.Fold();
        viewModel.Fold();
        viewModel.RaiseAmount = 18;
        viewModel.Raise();
        viewModel.Fold();
        viewModel.Call();
        viewModel.Board.QuickEntry = "ks8d3c";
        viewModel.Check();
        await viewModel.RefreshNowAsync();

        viewModel.StreetLabel.ShouldBe("Flop");
        viewModel.TurnLabel.ShouldContain("Your turn");
        viewModel.Recommendation.HasEvaluations.ShouldBeTrue();
    }

    /// <summary>
    /// Le second critère : en mode compact, le recalcul reste sous la seconde. La mesure est faite
    /// moteur chaud, sinon on chronomètre la compilation à la volée et non le calcul.
    /// </summary>
    [Fact]
    public async Task TheCompactModeAnswersInUnderASecond()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await ReachTheFlopAsync(viewModel, "Kh9d", "Ks8d3c");

        viewModel.ToggleCompact();
        await viewModel.RefreshNowAsync();

        long startedAt = Stopwatch.GetTimestamp();
        await viewModel.RefreshNowAsync();
        TimeSpan elapsed = Stopwatch.GetElapsedTime(startedAt);

        elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// La promesse de la reprise : fermer l'application au milieu d'une main et la rouvrir doit
    /// rendre exactement la même situation, cartes, board et actions comprises.
    /// </summary>
    [Fact]
    public async Task AHandInProgressSurvivesTheApplicationBeingClosed()
    {
        SessionStoreOptions session = NewSessionOptions();

        MainWindowViewModel before = await BuildAsync(session);
        await ReachTheFlopAsync(before, "Kh9d", "Ks8d3c");
        before.PersistNow();

        MainWindowViewModel after = await BuildAsync(session);

        after.Hero.AsHoleCards.ShouldBe(HoleCards.Parse("Kh9d"));
        after.Board.QuickEntry.ShouldBe("Ks8d3c");
        after.History.Count.ShouldBe(before.History.Count);
        after.StreetLabel.ShouldBe("Flop");
        after.Table.PlayerCount.ShouldBe(6);
    }

    /// <summary>
    /// Une main jouée sans ante porte un montant d'ante nul : l'appliquer telle quelle à la reprise
    /// effacerait le montant réglé, qui n'a rien à voir avec la main et resservira à la table suivante.
    /// </summary>
    [Fact]
    public async Task ResumingAHandPlayedWithoutAntesLeavesTheAnteSettingAlone()
    {
        SessionStoreOptions session = NewSessionOptions();

        MainWindowViewModel before = await BuildAsync(session);
        before.Table.AnteAmount = 25;
        before.Table.AnteStyle = AnteStyleChoice.All.Single(choice => choice.Value == AnteStyle.None);
        await ReachTheFlopAsync(before, "Kh9d", "Ks8d3c");
        before.PersistNow();

        MainWindowViewModel after = await BuildAsync(session);

        after.Table.AnteAmount.ShouldBe(25);
        after.Table.AnteStyle.Value.ShouldBe(AnteStyle.None);
    }

    [Fact]
    public async Task TheTableAndTheOpponentProfileAreRemembered()
    {
        SessionStoreOptions session = NewSessionOptions();

        MainWindowViewModel before = await BuildAsync(session);
        before.Table.PlayerCount = 3;
        before.Table.BigBlind = 50;
        before.Table.StartingStack = 1200;
        before.Table.AnteStyle = AnteStyleChoice.All.Single(choice => choice.Value == AnteStyle.BigBlindAnte);
        before.Profile = OpponentProfileChoice.All.Single(choice => choice.Value == OpponentProfile.Aggressive);
        before.ToggleCompact();
        before.PersistNow();

        MainWindowViewModel after = await BuildAsync(session);

        after.Table.PlayerCount.ShouldBe(3);
        after.Table.BigBlind.ShouldBe(50);
        after.Table.StartingStack.ShouldBe(1200);
        after.Table.AnteStyle.Value.ShouldBe(AnteStyle.BigBlindAnte);
        after.Profile.Value.ShouldBe(OpponentProfile.Aggressive);
        after.IsCompact.ShouldBeTrue();
    }

    [Fact]
    public async Task StartingANewHandArchivesThePreviousOne()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await ReachTheFlopAsync(viewModel, "Kh9d", "Ks8d3c");

        string advice = viewModel.Recommendation.Headline;
        viewModel.NewHand();
        await viewModel.RefreshNowAsync();

        JournalEntryViewModel entry = viewModel.Journal.Entries.ShouldHaveSingleItem();
        entry.Advice.ShouldBe(advice);
        entry.Hand.ShouldContain("K♥");
        entry.Hand.ShouldContain("BTN");

        viewModel.Hero.Selection.ShouldBeEmpty();
        viewModel.History.ShouldBeEmpty();
    }

    [Fact]
    public async Task ATableSetUpButNeverPlayedIsNotWorthJournalling()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await SelectAsync(viewModel, viewModel.Hero, "Ah", "Qd");

        viewModel.NewHand();
        await viewModel.RefreshNowAsync();

        viewModel.Journal.Entries.ShouldBeEmpty();
        viewModel.Journal.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public async Task TheJournalIsStillThereAfterARestart()
    {
        SessionStoreOptions session = NewSessionOptions();

        MainWindowViewModel before = await BuildAsync(session);
        await ReachTheFlopAsync(before, "Kh9d", "Ks8d3c");
        before.NewHand();
        await before.RefreshNowAsync();

        MainWindowViewModel after = await BuildAsync(session);

        after.Journal.Entries.ShouldHaveSingleItem().Hand.ShouldContain("K♥");
    }

    /// <summary>
    /// Ce qui distingue un journal d'un fichier de traces : l'entrée porte la main entière, donc on
    /// peut la remettre en place et rejouer la décision autrement.
    /// </summary>
    [Fact]
    public async Task AJournalledHandCanBePutBackOnTheTable()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await ReachTheFlopAsync(viewModel, "Kh9d", "Ks8d3c");
        viewModel.NewHand();
        await viewModel.RefreshNowAsync();

        viewModel.Journal.Replay(viewModel.Journal.Entries[0]);
        await viewModel.RefreshNowAsync();

        viewModel.Hero.AsHoleCards.ShouldBe(HoleCards.Parse("Kh9d"));
        viewModel.Board.QuickEntry.ShouldBe("Ks8d3c");
        viewModel.StreetLabel.ShouldBe("Flop");
        viewModel.Recommendation.HasEvaluations.ShouldBeTrue();
    }

    [Fact]
    public async Task EmptyingTheJournalLeavesNothingBehind()
    {
        SessionStoreOptions session = NewSessionOptions();

        MainWindowViewModel before = await BuildAsync(session);
        await ReachTheFlopAsync(before, "Kh9d", "Ks8d3c");
        before.NewHand();
        await before.RefreshNowAsync();

        before.Journal.Clear();

        before.Journal.Entries.ShouldBeEmpty();
        (await BuildAsync(session)).Journal.Entries.ShouldBeEmpty();
    }

    /// <summary>
    /// Le bouton de langue ne change pas que les boutons : le raisonnement du moteur, qui est le
    /// contenu même de l'écran, doit basculer avec eux — et revenir intact.
    /// </summary>
    [Fact]
    public async Task TheLanguageButtonSwitchesTheChromeAndTheReasoningAlike()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await ReachTheFlopAsync(viewModel, "Kh9d", "Ks8d3c");

        viewModel.Text.Fold.ShouldBe("Fold");
        viewModel.TurnLabel.ShouldContain("to act");
        viewModel.Recommendation.Rationale.ShouldContain(line => line.Contains("top pair", StringComparison.Ordinal));

        viewModel.ToggleLanguage();
        await viewModel.RefreshNowAsync();

        viewModel.Text.Fold.ShouldBe("Passer");
        viewModel.TurnLabel.ShouldContain("de parler");
        viewModel.Recommendation.Rationale.ShouldContain(line => line.Contains("top paire", StringComparison.Ordinal));
        viewModel.Matrix.Title.ShouldContain("Range attribuée");

        viewModel.ToggleLanguage();
        await viewModel.RefreshNowAsync();

        viewModel.Text.Fold.ShouldBe("Fold");
        viewModel.Recommendation.Rationale.ShouldContain(line => line.Contains("top pair", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SwitchingLanguageKeepsTheHandOnTheTable()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await ReachTheFlopAsync(viewModel, "Kh9d", "Ks8d3c");

        int actions = viewModel.History.Count;

        viewModel.ToggleLanguage();
        await viewModel.RefreshNowAsync();

        viewModel.Hero.AsHoleCards.ShouldBe(HoleCards.Parse("Kh9d"));
        viewModel.Board.QuickEntry.ShouldBe("Ks8d3c");
        viewModel.History.Count.ShouldBe(actions);
        viewModel.Recommendation.HasEvaluations.ShouldBeTrue();

        // Le déroulé est réécrit, pas seulement conservé : ses phrases datent de la saisie.
        viewModel.History[0].Label.ShouldBe("UTG passe");
    }

    [Fact]
    public async Task TheChosenLanguageIsRememberedForNextTime()
    {
        SessionStoreOptions session = NewSessionOptions();

        MainWindowViewModel before = await BuildAsync(session);
        before.ToggleLanguage();
        before.PersistNow();

        MainWindowViewModel after = await BuildAsync(session);

        after.Text.Fold.ShouldBe("Passer");
        after.Table.Text.Players.ShouldBe("Joueurs");

        // Ce libellé est calculé à la construction, avant que la langue enregistrée ne soit connue.
        after.Table.DepthLabel.ShouldContain("de profondeur");
    }

    /// <summary>
    /// Le profil est enregistré sous son nom traduit : le relire dans l'autre langue ne doit pas
    /// faire retomber l'utilisateur sur le profil par défaut.
    /// </summary>
    [Fact]
    public async Task AProfileChosenInOneLanguageSurvivesTheSwitch()
    {
        SessionStoreOptions session = NewSessionOptions();

        MainWindowViewModel before = await BuildAsync(session);
        before.Profile = OpponentProfileChoice.All.Single(choice => choice.Value == OpponentProfile.Tight);
        before.ToggleLanguage();
        before.PersistNow();

        MainWindowViewModel after = await BuildAsync(session);

        after.Profile.Value.ShouldBeSameAs(OpponentProfile.Tight);
        after.Profile.Label.ShouldBe("Serré");
    }

    /// <summary>
    /// Postflop la grille montre la range de l'adversaire, pas la sienne : la légende doit donc
    /// parler de combos et non d'actions. Lire « passe » sur une case grise, c'est croire qu'on
    /// lit une décision alors qu'on lit une impossibilité.
    /// </summary>
    [Fact]
    public async Task TheLegendSaysWhatTheGridIsActuallyShowing()
    {
        MainWindowViewModel viewModel = await BuildAsync();

        viewModel.Matrix.Legend.Select(entry => entry.Label).ShouldContain("Fold");

        await ReachTheFlopAsync(viewModel, "Kh9d", "Ks8d3c");

        viewModel.Matrix.Legend.Select(entry => entry.Label).ShouldNotContain("Fold");
        viewModel.Matrix.Legend.Select(entry => entry.Label).ShouldContain("He cannot have it");
    }

    /// <summary>
    /// K♦A♥ sur K♣9♣7♥ : la case AKo est éteinte parce que la grosse blinde aurait 3-bet cette
    /// main, pas parce que le calcul a échoué. L'écran doit le dire.
    /// </summary>
    [Fact]
    public async Task AHeroHandOutsideTheOpponentRangeIsExplainedNotJustGreyed()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await ReachHeadsUpFlopAsync(viewModel, "KdAh", "Kc9c7h");

        viewModel.Matrix.Cells.Single(cell => cell.IsHeroHand).Label.ShouldBe("AKo");

        viewModel.Matrix.HasHeroNote.ShouldBeTrue();
        viewModel.Matrix.HeroNote!.ShouldContain("AKo");
        viewModel.Matrix.HeroNote!.ShouldContain("not in the range assigned to BB");
    }

    /// <summary>
    /// 7♥7♦ sur 8♥3♣7♠ : les six combos de 77 sont pris — deux dans ta main, un au board — donc
    /// l'adversaire ne peut pas l'avoir. C'est une autre cause qu'une absence de range.
    /// </summary>
    [Fact]
    public async Task AHeroHandWhoseCombosAreAllBlockedSaysSo()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await ReachHeadsUpFlopAsync(viewModel, "7h7d", "8h3c7s");

        viewModel.Matrix.Cells.Single(cell => cell.IsHeroHand).Label.ShouldBe("77");

        viewModel.Matrix.HasHeroNote.ShouldBeTrue();
        viewModel.Matrix.HeroNote!.ShouldContain("none of its 6 combos");
    }

    /// <summary>
    /// Le corollaire : une case partiellement bloquée reste colorée. Sans cela « éteint » voudrait
    /// dire à la fois « absent » et « entamé », et la grille ne dirait plus rien.
    /// </summary>
    [Fact]
    public async Task ACellTheBoardOnlyPartlyBlocksKeepsItsColour()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        await ReachHeadsUpFlopAsync(viewModel, "7h7d", "8h3c7s");

        // Le board occupe un 8 sur quatre : la moitié des six combos de 88 restent possibles.
        viewModel.Matrix.Cells.Single(cell => cell.Label == "88").Tooltip.ShouldContain("50");
    }

    [Fact]
    public async Task AHeroHandInsideTheOpponentRangeNeedsNoExplanation()
    {
        MainWindowViewModel viewModel = await BuildAsync();
        // QTo est dans la range de suivi de la grosse blinde, et rien ne bloque ses combos.
        await ReachHeadsUpFlopAsync(viewModel, "QhTd", "Kc9c7h");

        viewModel.Matrix.Cells.Single(cell => cell.IsHeroHand).Label.ShouldBe("QTo");
        viewModel.Matrix.HasHeroNote.ShouldBeFalse();
    }

    /// <summary>Tête-à-tête : la petite blinde ouvre, la grosse blinde suit, puis le flop tombe.</summary>
    private static async Task ReachHeadsUpFlopAsync(MainWindowViewModel viewModel, string heroCards, string board)
    {
        viewModel.Table.PlayerCount = 2;
        viewModel.Table.BigBlind = 120;
        viewModel.Table.StartingStack = 3220;
        viewModel.Table.HeroPosition = Choice(viewModel, Position.SmallBlind);

        viewModel.Hero.QuickEntry = heroCards;
        viewModel.RaiseAmount = 360;
        viewModel.Raise();
        viewModel.Call();
        viewModel.Board.QuickEntry = board;

        await viewModel.RefreshNowAsync();
    }

    private static async Task ReachTheFlopAsync(MainWindowViewModel viewModel, string heroCards, string board)
    {
        viewModel.Table.PlayerCount = 6;
        viewModel.Table.StartingStack = 800;
        viewModel.Table.HeroPosition = Choice(viewModel, Position.Button);
        await SelectAsync(viewModel, viewModel.Hero, heroCards[..2], heroCards[2..]);

        viewModel.Fold();
        viewModel.Fold();
        viewModel.Fold();
        viewModel.RaiseAmount = 18;
        viewModel.Raise();
        viewModel.Fold();
        viewModel.Call();

        await SelectAsync(viewModel, viewModel.Board, board[..2], board[2..4], board[4..]);
    }

    private static PositionChoice Choice(MainWindowViewModel viewModel, Position position)
    {
        return viewModel.Table.AvailablePositions.Single(choice => choice.Value == position);
    }

    private static CardOptionViewModel Option(CardPickerViewModel picker, string card)
    {
        Card parsed = Card.Parse(card);
        return picker.Cards.Single(option => option.Card == parsed);
    }

    private static async Task SelectAsync(MainWindowViewModel viewModel, CardPickerViewModel picker, params string[] cards)
    {
        foreach (string card in cards)
        {
            picker.Toggle(Option(picker, card));
        }

        await viewModel.RefreshNowAsync();
    }

    private Task<MainWindowViewModel> BuildAsync()
    {
        return BuildAsync(NewSessionOptions());
    }

    private SessionStoreOptions NewSessionOptions()
    {
        string directory = Path.Combine(Path.GetTempPath(), "PokerRanges.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        _directories.Add(directory);

        return new SessionStoreOptions
        {
            PreferencesFilePath = Path.Combine(directory, "settings.json"),
            HandFilePath = Path.Combine(directory, "hand.json"),
            JournalFilePath = Path.Combine(directory, "journal.json"),
        };
    }

    /// <summary>
    /// Les tests montent les vrais magasins JSON dans un dossier temporaire plutôt que des doublures :
    /// la promesse à vérifier est qu'une main survit à la fermeture, ce qu'un faux en mémoire ne dit
    /// pas. Repasser les mêmes options simule un relancement de l'application.
    /// </summary>
    private static async Task<MainWindowViewModel> BuildAsync(SessionStoreOptions session)
    {
        RankCountHandEvaluator evaluator = new();
        PotEngine potEngine = new(NullLogger<PotEngine>.Instance);
        JsonPreflopChartRepository charts = new(
            PreflopChartRepositoryOptions.EmbeddedOnly,
            NullLogger<JsonPreflopChartRepository>.Instance);
        RangeStrengthRanker ranker = new(evaluator, PostflopOptions.Default, NullLogger<RangeStrengthRanker>.Instance);

        MainWindowViewModel viewModel = new(
            potEngine,
            new AdviceCoordinator(
                new PreflopAdvisor(charts, potEngine, PreflopAdvisorOptions.Default, NullLogger<PreflopAdvisor>.Instance),
                new EvPostflopAdvisor(
                    potEngine,
                    new RangeAssigner(charts, potEngine, ranker, PreflopAdvisorOptions.Default, NullLogger<RangeAssigner>.Instance),
                    ranker,
                    new MadeHandClassifier(evaluator),
                    new EquityCalculator(evaluator, NullLogger<EquityCalculator>.Instance),
                    PostflopOptions.Default,
                    NullLogger<EvPostflopAdvisor>.Instance),
                NullLogger<AdviceCoordinator>.Instance),
            new JsonSessionStore(session, NullLogger<JsonSessionStore>.Instance),
            new ChartsViewModel(charts, NullLogger<ChartsViewModel>.Instance),
            new JournalViewModel(
                new JsonHandJournal(session, NullLogger<JsonHandJournal>.Instance),
                NullLogger<JournalViewModel>.Instance),
            NullLogger<MainWindowViewModel>.Instance);

        await viewModel.RefreshNowAsync();

        return viewModel;
    }
}
