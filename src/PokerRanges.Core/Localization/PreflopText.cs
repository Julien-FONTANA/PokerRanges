using System.Globalization;

namespace PokerRanges.Core.Localization;

public static class PreflopText
{
    public static string ContextRaiseFirstIn => Language.Pick("Open-raise", "Ouverture");

    public static string ContextVersusLimp => Language.Pick("Facing one or more limps", "Face à un ou plusieurs limps");

    public static string ContextVersusOpen => Language.Pick("Facing an open", "Face à une ouverture");

    public static string ContextSqueeze => Language.Pick("Squeeze", "Squeeze");

    public static string ContextVersusThreeBet => Language.Pick("Facing a 3bet", "Face à un 3bet");

    public static string ContextVersusFourBet => Language.Pick("Facing a 4bet", "Face à un 4bet");

    public static string ContextJam => Language.Pick("Open jam", "Tapis d'ouverture");

    public static string ContextCallJam => Language.Pick("Facing a jam", "Face à un tapis");

    public static string RelationInPosition => Language.Pick("in position", "en position");

    public static string RelationOutOfPosition => Language.Pick("out of position", "hors de position");

    public static string RelationSmallBlind => Language.Pick("in the small blind", "en petite blinde");

    public static string RelationBigBlind => Language.Pick("in the big blind", "en grosse blinde");

    public static string OptionFold => Language.Pick("Fold", "Passer");

    public static string OptionCall => Language.Pick("Call", "Suivre");

    public static string OptionJam => Language.Pick("Jam", "Tapis");

    public static string OptionRaise => Language.Pick("Raise", "Relancer");

    public static string OptionRaiseTo(double sizeInBigBlinds)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Raise to {sizeInBigBlinds:0.##}bb"),
            string.Create(CultureInfo.CurrentCulture, $"Relancer à {sizeInBigBlinds:0.##}bb"));
    }

    public static string ChartSummary(string context, string relation, int playersLeftToAct, double depthInBigBlinds)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{context},{relation} {playersLeftToAct} player(s) behind, {depthInBigBlinds:0.#}bb"),
            string.Create(CultureInfo.CurrentCulture, $"{context},{relation} {playersLeftToAct} joueur(s) derrière, {depthInBigBlinds:0.#}bb"));
    }

    public static string ChartPrefix(string summary)
    {
        return Language.Pick($"Chart: {summary}", $"Chart : {summary}");
    }

    public static string ChartPrefixWithAdjustments(string summary, string adjustments)
    {
        return Language.Pick(
            $"Chart: {summary} — {adjustments}",
            $"Chart : {summary} — {adjustments}");
    }

    public static string AdjustmentSeparator => Language.Pick("; ", " ; ");

    public static string FallbackContext(string missing, string used)
    {
        return Language.Pick(
            $"no chart for {missing}, falling back to {used} — take this advice with caution",
            $"aucun chart pour {missing}, repli sur {used} — conseil à prendre avec prudence");
    }

    public static string FallbackRelation(string requested, string used)
    {
        return Language.Pick(
            $"{requested} unavailable, {used} chart used",
            $"relation {requested} indisponible, chart {used} utilisé");
    }

    public static string FallbackPlayersLeft(int requested, int used)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{requested} player(s) behind requested, chart has {used}"),
            string.Create(CultureInfo.CurrentCulture, $"{requested} joueur(s) derrière demandé(s), chart à {used}"));
    }

    public static string FallbackDepth(double requested, double used)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{requested:0.#}bb requested, chart at {used:0.#}bb"),
            string.Create(CultureInfo.CurrentCulture, $"profondeur {requested:0.#}bb demandée, chart à {used:0.#}bb"));
    }

    public static string NoChartCovers(string situation)
    {
        return Language.Pick(
            $"No chart covers the situation « {situation} », nor any of its fallbacks.",
            $"Aucun chart ne couvre la situation « {situation} », ni aucun de ses replis.");
    }

    public static string HeroCardsRequired => Language.Pick(
        "Both of your cards are needed before any advice can be given.",
        "Les deux cartes du héros doivent être renseignées pour obtenir un conseil.");

    public static string RationaleSeat(string handClass, string position, int playerCount, int playersLeftToAct)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{handClass} in {position}, {playerCount}-handed table, {playersLeftToAct} player(s) behind."),
            string.Create(CultureInfo.CurrentCulture, $"{handClass} en {position} à une table de {playerCount}, {playersLeftToAct} joueur(s) derrière."));
    }

    public static string RationaleDepth(double depthInBigBlinds, double potInBigBlinds)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Effective depth: {depthInBigBlinds:0.#}bb. Pot: {potInBigBlinds:0.#}bb."),
            string.Create(CultureInfo.CurrentCulture, $"Profondeur effective : {depthInBigBlinds:0.#}bb. Pot : {potInBigBlinds:0.#}bb."));
    }

    public static string RationaleAggressor(string aggressor, string relation)
    {
        return Language.Pick(
            $"Facing aggression from {aggressor}{relation}.",
            $"Face à l'agression de {aggressor}{relation}.");
    }

    public static string RationaleToCall(double amountInBigBlinds, double requiredEquity)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"To call: {amountInBigBlinds:0.#}bb, so {requiredEquity:P1} equity is needed."),
            string.Create(CultureInfo.CurrentCulture, $"À payer : {amountInBigBlinds:0.#}bb, soit {requiredEquity:P1} d'équité nécessaire pour suivre."));
    }

    public static string RationaleChartStrategy(string options)
    {
        return Language.Pick($"Chart strategy: {options}", $"Stratégie du chart : {options}");
    }
}
