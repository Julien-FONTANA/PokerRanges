using CommunityToolkit.Mvvm.ComponentModel;
using PokerRanges.Core.Localization;

namespace PokerRanges.App.Localization;

/// <summary>
/// The interface labels. A single observable object rather than constants: views bind to it with
/// <c>{Binding Text.Something}</c>, and a language change notifies "all my properties changed",
/// which refreshes the entire screen with no restart and no re-reading.
/// </summary>
public sealed partial class UiText : ObservableObject
{
    private UiText()
    {
        Language.Changed += (_, _) => OnPropertyChanged(string.Empty);
    }

    public static UiText Current { get; } = new();

    public string WindowTitle => Language.Pick(
        "PokerRanges — MTT decision assistant",
        "PokerRanges — assistant de décision MTT");

    public string Players => Language.Pick("Players", "Joueurs");

    public string BigBlind => Language.Pick("Big blind", "Grosse blinde");

    public string StartingStack => Language.Pick("Starting stack", "Tapis de départ");

    public string Ante => Language.Pick("Ante", "Ante");

    public string MyPosition => Language.Pick("My position", "Ma position");

    public string OpponentProfile => Language.Pick("Opponent profile", "Profil adverse");

    public string Display => Language.Pick("Display", "Affichage");

    public string CompactMode => Language.Pick("Compact mode  (F2)", "Mode compact  (F2)");

    public string AnalysisMode => Language.Pick("Analysis", "Analyse");

    public string LanguageToggle => Language.Pick("Français", "English");

    public string LanguageHeader => Language.Pick("Language", "Langue");

    public string MyHand => Language.Pick("MY HAND", "MA MAIN");

    public string Board => Language.Pick("BOARD", "BOARD");

    public string HandLabel => Language.Pick("Hand", "Main");

    public string BoardLabel => Language.Pick("Board", "Board");

    public string NoHand => Language.Pick("No hand", "Aucune main");

    public string EmptyBoard => Language.Pick("Empty board", "Board vide");

    public string Clear => Language.Pick("Clear", "Effacer");

    public string KeyboardHint => Language.Pick("type it: askd", "au clavier : askd");

    public string Fold => Language.Pick("Fold", "Passer");

    public string Check => Language.Pick("Check", "Checker");

    public string Call => Language.Pick("Call", "Suivre");

    public string BetOrRaiseTo => Language.Pick("Bet / raise to", "Miser / relancer à");

    public string Bet => Language.Pick("Bet", "Miser");

    public string UndoLast => Language.Pick("Undo last", "Annuler la dernière");

    public string NewHand => Language.Pick("New hand", "Nouvelle main");

    public string NewHandShort => Language.Pick("New", "Nouvelle");

    public string HandHistory => Language.Pick("Hand history", "Déroulé de la main");

    public string Journal => Language.Pick("HAND JOURNAL", "JOURNAL DES MAINS");

    public string Replay => Language.Pick("Replay", "Rejouer");

    public string Empty => Language.Pick("Empty", "Vider");

    public string Charts => Language.Pick("PREFLOP CHARTS", "CHARTS PRÉFLOP");

    public string ChartsHint => Language.Pick(
        "Edit a file in that folder, then reload to see the advice change.",
        "Modifie un fichier du dossier, puis recharge pour voir le conseil changer.");

    public string Reload => Language.Pick("Reload", "Recharger");

    public string RestoreDefaults => Language.Pick("Restore originals", "Restaurer l'origine");

    public string ExpectedValueByAction => Language.Pick("Expected value by action", "Espérance par action");

    public string Why => Language.Pick("Why", "Pourquoi");

    public string Waiting => Language.Pick("Waiting", "En attente");

    public string MixedStrategy => Language.Pick(
        "Mixed strategy: several actions are playable with this hand.",
        "Stratégie mixte : plusieurs actions sont jouables sur cette main.");

    public string CloseDecision => Language.Pick(
        "Close decision: the runner-up is within model noise.",
        "Décision serrée : la deuxième option est à portée de bruit de modèle.");

    public string MultiwayCaveat => Language.Pick(
        "Multiway pot: the opponent's re-raise is not modelled, so bet expectations are approximate.",
        "Pot multi-joueurs : la re-relance adverse n'est pas modélisée, l'espérance des mises est approchée.");

    public string PickTwoCards => Language.Pick(
        "Pick your two cards to get advice.",
        "Sélectionne tes deux cartes pour obtenir un conseil.");

    public string PickTwoCardsPostflop => Language.Pick(
        "Pick your two cards to analyse the hand.",
        "Sélectionne tes deux cartes pour analyser le coup.");

    public string YouFolded => Language.Pick("You folded this hand.", "Tu as passé cette main.");

    public string EmptyJournal => Language.Pick(
        "No hand recorded. A hand goes to the journal when you start a new one.",
        "Aucune main enregistrée. Une main part au journal quand tu en commences une nouvelle.");

    public string Shortcuts => Language.Pick(
        "Alt+P fold · Alt+C check · Alt+S call · Alt+R bet · Ctrl+Z undo · Ctrl+N new hand · F2 compact mode",
        "Alt+P passer · Alt+C checker · Alt+S suivre · Alt+R miser · Ctrl+Z annuler · Ctrl+N nouvelle main · F2 mode compact");

    public string LegendJam => Language.Pick("All-in", "Tapis");

    public string LegendRaise => Language.Pick("Raise", "Relance");

    public string LegendCall => Language.Pick("Call", "Suivi");

    public string LegendFold => Language.Pick("Fold", "Passe");

    public string LegendAllCombos => Language.Pick("All his combos", "Tous ses combos");

    public string LegendHalfCombos => Language.Pick("Some of them", "Une partie");

    public string LegendNoCombos => Language.Pick("He cannot have it", "Il ne peut pas l'avoir");

    public string AnteNone => Language.Pick("No ante", "Aucune ante");

    public string AnteBigBlind => Language.Pick("Big blind ante", "Ante payée par la BB");

    public string AntePerPlayer => Language.Pick("Ante per player", "Ante par joueur");
}
