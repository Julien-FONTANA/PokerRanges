using System.Globalization;

namespace PokerRanges.Core.Localization;

public static class PostflopText
{
    public static string ProfileBalanced => Language.Pick("Balanced", "Équilibré");

    public static string ProfileTight => Language.Pick("Tight", "Serré");

    public static string ProfileCallingStation => Language.Pick("Calling station", "Suiveur");

    public static string ProfileAggressive => Language.Pick("Aggressive", "Agressif");

    public static string BudgetFull => Language.Pick("Analysis", "Analyse");

    public static string BudgetFast => Language.Pick("Fast", "Rapide");

    public static string ActionFold => Language.Pick("Fold", "Passer");

    public static string ActionCheck => Language.Pick("Check", "Checker");

    public static string ActionCall(double amount)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Call {amount:0.##}"),
            string.Create(CultureInfo.CurrentCulture, $"Suivre {amount:0.##}"));
    }

    public static string ActionBet(string sizing)
    {
        return Language.Pick($"Bet {sizing}", $"Miser {sizing}");
    }

    public static string ActionRaise(string sizing)
    {
        return Language.Pick($"Raise to {sizing}", $"Relancer à {sizing}");
    }

    public static string Sizing(double amount, double fractionOfPot)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{amount:0.##} ({fractionOfPot:P0} of pot)"),
            string.Create(CultureInfo.CurrentCulture, $"{amount:0.##} ({fractionOfPot:P0} du pot)"));
    }

    public static string FoldExplanation => Language.Pick(
        "We give up the pot; what is already in it is lost either way.",
        "On abandonne le pot ; ce qui y est déjà engagé est perdu quoi qu'il arrive.");

    public static string CheckExplanation(double equity, double pot, double realisation)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{equity:P1} equity in a {pot:0.##} pot, of which about {realisation:P0} is realised by playing passively."),
            string.Create(CultureInfo.CurrentCulture, $"{equity:P1} d'équité sur un pot de {pot:0.##}, dont on encaisse environ {realisation:P0} en jouant passivement."));
    }

    public static string CallExplanation(double equity, double requiredEquity)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{equity:P1} equity against the {requiredEquity:P1} required"),
            string.Create(CultureInfo.CurrentCulture, $"{equity:P1} d'équité pour {requiredEquity:P1} requises"));
    }

    public static string CallImpliedOdds(double implied)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $", plus {implied:0.##} of implied odds on the draw."),
            string.Create(CultureInfo.CurrentCulture, $", plus {implied:0.##} de cotes implicites sur le tirage."));
    }

    public static string AggressionExplanation(double foldProbability, double equityWhenCalled)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"He folds {foldProbability:P0} of the time; when he continues, we hold {equityWhenCalled:P1} against his continuing range."),
            string.Create(CultureInfo.CurrentCulture, $"Il se couche {foldProbability:P0} du temps ; quand il continue, on a {equityWhenCalled:P1} d'équité contre sa range de continuation."));
    }

    public static string RationaleHand(string hand)
    {
        return Language.Pick($"Your hand: {hand}.", $"Ta main : {hand}.");
    }

    public static string RationalePot(double pot, double potInBigBlinds, double effectiveStack, double spr)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Pot {pot:0.##} ({potInBigBlinds:0.#}bb), effective stack {effectiveStack:0.##}, SPR {spr:0.#}."),
            string.Create(CultureInfo.CurrentCulture, $"Pot {pot:0.##} ({potInBigBlinds:0.#}bb), tapis effectif {effectiveStack:0.##}, SPR {spr:0.#}."));
    }

    public static string RationaleFacingBet(double amountToCall, double requiredEquity, double minimumDefence)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"To call {amountToCall:0.##}: {requiredEquity:P1} equity is needed, and he must defend {minimumDefence:P0} of his range not to be exploitable."),
            string.Create(CultureInfo.CurrentCulture, $"À payer {amountToCall:0.##} : il faut {requiredEquity:P1} d'équité, et défendre {minimumDefence:P0} de sa range pour ne pas être exploitable."));
    }

    public static string RationaleOuts(int outs, double improvementChance)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{outs} outs, i.e. {improvementChance:P0} to improve by the river."),
            string.Create(CultureInfo.CurrentCulture, $"{outs} outs, soit {improvementChance:P0} d'amélioration d'ici la river."));
    }

    public static string RationaleOpponentModel(string profile)
    {
        return Language.Pick($"Opponent model: {profile}.", $"Modèle d'adversaire : {profile}.");
    }

    public static string RationaleCloseCall => Language.Pick(
        "The runner-up is within model noise: both are defensible.",
        "La deuxième option est à portée de bruit de modèle : les deux se défendent.");

    public static string StoryNoActionYet(string position)
    {
        return Language.Pick(
            $"{position} has not acted preflop yet: full range assumed.",
            $"{position} n'a pas encore agi préflop : range complète retenue.");
    }

    public static string StoryOpening(string position, string how, string chart)
    {
        return Language.Pick($"{position}: {how} — {chart}", $"{position} : {how} — {chart}");
    }

    public static string StoryRaisedPreflop => Language.Pick("raised preflop", "a relancé préflop");

    public static string StoryCalledPreflop => Language.Pick("called preflop", "a suivi préflop");

    public static string StoryCheckedOption => Language.Pick(
        "checked his option in the big blind",
        "a checké son option en grosse blinde");

    public static string StoryActionMissingFromChart(string action)
    {
        return Language.Pick(
            $"{action} preflop, an action absent from the chart: the chart's whole range is kept",
            $"a {action} préflop, action absente du chart : range complète du chart retenue");
    }

    public static string ActionFolded => Language.Pick("folded", "passé");

    public static string ActionChecked => Language.Pick("checked", "checké");

    public static string ActionCalled => Language.Pick("called", "suivi");

    public static string ActionBetted => Language.Pick("bet", "misé");

    public static string ActionRaised => Language.Pick("raised", "relancé");

    public static string StoryCheckKeepsRange(string street)
    {
        return Language.Pick(
            $"{street}: check, range unchanged.",
            $"{street} : check, range inchangée.");
    }

    public static string StoryPolarised(string street, double amount, double combos)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{street}: bet or raise to {amount:0.##}, range polarised to {combos:0.#} combos."),
            string.Create(CultureInfo.CurrentCulture, $"{street} : mise ou relance à {amount:0.##}, range polarisée à {combos:0.#} combos."));
    }

    public static string StoryCalls(string street, double amountToCall, double callProbability)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{street}: calls {amountToCall:0.##}, keeping only {callProbability:P0} of his range."),
            string.Create(CultureInfo.CurrentCulture, $"{street} : suit {amountToCall:0.##}, il ne garde que {callProbability:P0} de sa range."));
    }

    public static string StoryFinalRange(double combos, double percentOfAllHands)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Range kept: {combos:0.#} combos ({percentOfAllHands:0.#} % of all hands)."),
            string.Create(CultureInfo.CurrentCulture, $"Range retenue : {combos:0.#} combos ({percentOfAllHands:0.#} % des mains)."));
    }

    public static string Precision(string budget, double margin, double milliseconds)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{budget} budget — equity to within ±{margin:P1}, computed in {milliseconds:0} ms. The range ranking is sampled too: this margin does not account for it."),
            string.Create(CultureInfo.CurrentCulture, $"Budget {budget} — équité à ±{margin:P1} près, calculé en {milliseconds:0} ms. Le classement de range est lui aussi échantillonné : cette marge ne le compte pas."));
    }

    public static string BudgetSummary(string name, int equitySamples, int rankingSamples)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{name} budget: {equitySamples:N0} equity samples, {rankingSamples} per combo for the ranking."),
            string.Create(CultureInfo.CurrentCulture, $"Budget {name} : {equitySamples:N0} tirages d'équité, {rankingSamples} par combo pour le classement."));
    }

    public static string HeroCardsRequired => Language.Pick(
        "Both of your cards must be filled in.",
        "Les deux cartes du héros doivent être renseignées.");

    public static string FlopRequired(int boardCardCount)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Postflop advice needs at least a flop, got a board of {boardCardCount} cards."),
            string.Create(CultureInfo.CurrentCulture, $"Le conseil postflop attend au moins un flop, board de {boardCardCount} cartes reçu."));
    }

    public static string NoOpponentLeft => Language.Pick(
        "There is no opponent left in the hand.",
        "Il n'y a plus d'adversaire dans le coup.");
}
