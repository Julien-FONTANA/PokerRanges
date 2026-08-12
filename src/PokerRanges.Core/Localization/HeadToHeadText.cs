using System.Globalization;

namespace PokerRanges.Core.Localization;

public static class HeadToHeadText
{
    public static string ActionJam(double amount)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Jam {amount:0.##}"),
            string.Create(CultureInfo.CurrentCulture, $"Tapis {amount:0.##}"));
    }

    public static string JamExplanation(double foldProbability, double equity)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"He folds {foldProbability:P0} of the time; when he calls we hold {equity:P1}."),
            string.Create(CultureInfo.CurrentCulture, $"Il passe {foldProbability:P0} du temps ; quand il suit, nous détenons {equity:P1}."));
    }

    public static string CallExplanation(double equity, double requiredEquity)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{equity:P1} equity against the jamming range, and {requiredEquity:P1} is what the price asks."),
            string.Create(CultureInfo.CurrentCulture, $"{equity:P1} d'équité face à la range de tapis, et le prix en réclame {requiredEquity:P1}."));
    }

    public static string Showdown => Language.Pick("Showdown", "Abattage");

    public static string ShowdownExplanation(double equity)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Already all-in: {equity:P1} of the pot, whatever we would rather do."),
            string.Create(CultureInfo.CurrentCulture, $"Déjà à tapis : {equity:P1} du pot, quoi que nous préférions faire."));
    }

    public static string SpotSummary(double effectiveStack, double depthInBigBlinds, double contestedPot)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Effective stack {effectiveStack:0.##} chips ({depthInBigBlinds:0.#}bb); {contestedPot:0.##} contested at showdown."),
            string.Create(CultureInfo.CurrentCulture, $"Tapis effectif {effectiveStack:0.##} jetons ({depthInBigBlinds:0.#}bb) ; {contestedPot:0.##} en jeu à l'abattage."));
    }

    public static string DeadMoney(double deadChips)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{deadChips:0.##} chips of dead money left by the players who folded."),
            string.Create(CultureInfo.CurrentCulture, $"{deadChips:0.##} jetons d'argent mort laissés par les joueurs qui ont passé."));
    }

    public static string BreakEvenEquity(double requiredEquity, double heroRisk)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Risking {heroRisk:0.##} needs {requiredEquity:P1} to break even once called."),
            string.Create(CultureInfo.CurrentCulture, $"Risquer {heroRisk:0.##} demande {requiredEquity:P1} pour être à l'équilibre une fois suivi."));
    }

    public static string BreakEvenFoldFrequency(double frequency)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"The jam breaks even if he folds {frequency:P0} of the time."),
            string.Create(CultureInfo.CurrentCulture, $"Le tapis est à l'équilibre s'il passe {frequency:P0} du temps."));
    }

    public static string JamProfitableWithoutAnyFold => Language.Pick(
        "The jam shows a profit even if he never folds: it does not need fold equity at all.",
        "Le tapis est profitable même s'il ne passe jamais : il n'a aucun besoin d'équité de fold.");

    public static string VillainContinues(double frequency, double combos)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"His calling range is {combos:0.#} combos, i.e. {frequency:P1} of the hands he can hold."),
            string.Create(CultureInfo.CurrentCulture, $"Sa range de call fait {combos:0.#} combos, soit {frequency:P1} des mains qu'il peut détenir."));
    }

    public static string ChipsNotIcm => Language.Pick(
        "Every figure here is in chips. At a final table that is the wrong currency: near a pay jump, survival is worth more than the chips say.",
        "Tous les chiffres ici sont en jetons. À une table finale c'est la mauvaise monnaie : près d'un palier, la survie vaut plus que ne le disent les jetons.");

    public static string HeroCardRemovalIgnored => Language.Pick(
        "Your side is a range, so his fold frequency is counted without removing your own cards from it.",
        "Votre camp est une range : sa fréquence de fold est comptée sans en retirer vos propres cartes.");

    public static string VillainPinnedToOneHand => Language.Pick(
        "He is pinned to a single hand, so he never folds: what is left is the showdown alone.",
        "Il est fixé à une seule main, donc il ne passe jamais : il ne reste que l'abattage.");

    public static string VillainCannotFold => Language.Pick(
        "He is already all-in and cannot fold, so there is no fold equity to collect.",
        "Il est déjà à tapis et ne peut pas passer : il n'y a aucune équité de fold à encaisser.");

    public static string HeroCannotFold => Language.Pick(
        "You are already all-in: there is no decision left, only the showdown.",
        "Vous êtes déjà à tapis : il ne reste aucune décision, seulement l'abattage.");

    public static string JammingRangeIsTheVillainsCall => Language.Pick(
        "His range is read as the hands he calls the jam with.",
        "Sa range est lue comme les mains avec lesquelles il suit le tapis.");

    public static string CallingRangeIsTheVillainsJam => Language.Pick(
        "His range is read as the hands he jams with, so no fold frequency applies.",
        "Sa range est lue comme les mains avec lesquelles il fait tapis : aucune fréquence de fold ne s'applique.");

    public static string Precision(double margin, double milliseconds)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Equity to within ±{margin:P1}, computed in {milliseconds:0} ms."),
            string.Create(CultureInfo.CurrentCulture, $"Équité à ±{margin:P1} près, calculée en {milliseconds:0} ms."));
    }

    public static string PrecisionExhaustive(double milliseconds)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Every runout enumerated: the equity is exact. Computed in {milliseconds:0} ms."),
            string.Create(CultureInfo.CurrentCulture, $"Tous les tableaux énumérés : l'équité est exacte. Calculée en {milliseconds:0} ms."));
    }

    public static string VillainMustBeAnotherSeat => Language.Pick(
        "The opponent has to be a different seat from the hero's.",
        "L'adversaire doit occuper un siège différent de celui du héros.");

    public static string VillainNotSeated(int playerCount)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"The chosen opponent is not seated at a {playerCount}-player table."),
            string.Create(CultureInfo.CurrentCulture, $"L'adversaire choisi n'est pas assis à une table de {playerCount} joueurs."));
    }

    public static string NoEffectiveStack => Language.Pick(
        "There is nothing to play for: one of the two stacks is empty.",
        "Il n'y a rien à jouer : l'un des deux tapis est vide.");

    public static string OpponentMustBeAlone(int liveOpponents)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"A head-to-head spot needs exactly one opponent still in the hand, not {liveOpponents}."),
            string.Create(CultureInfo.CurrentCulture, $"Un tête-à-tête demande exactement un adversaire encore dans le coup, pas {liveOpponents}."));
    }

    public static string EmptyHeroRange => Language.Pick(
        "Your range holds no hand once the board and his cards are removed.",
        "Votre range ne contient aucune main une fois le tableau et ses cartes retirés.");

    public static string EmptyVillainRange => Language.Pick(
        "His range holds no hand once the board and your cards are removed.",
        "Sa range ne contient aucune main une fois le tableau et vos cartes retirés.");
}
