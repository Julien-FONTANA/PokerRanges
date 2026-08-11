using System.Globalization;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Table;

namespace PokerRanges.App.Localization;

/// <summary>
/// Les phrases construites de l'interface — celles qui prennent des valeurs. Séparées de
/// <see cref="UiText"/>, qui ne porte que des libellés fixes liables directement depuis le XAML.
/// </summary>
public static class UiMatrixText
{
    public static string GridPlaceholderTitle => Language.Pick("Situation grid", "Grille de la situation");

    public static string GridPlaceholderSubtitle => Language.Pick(
        "Fill in the table to display a chart.",
        "Renseigne la table pour afficher un chart.");

    public static string CellShare(HandClass handClass, double frequency)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{handClass}: {frequency:P0} of the cell"),
            string.Create(CultureInfo.CurrentCulture, $"{handClass} : {frequency:P0} de la case"));
    }

    public static string CellOption(string option, string frequency)
    {
        return Language.Pick($"{option}: {frequency}", $"{option} : {frequency}");
    }

    public static string PreflopGridTitle(string context, string position, double depthInBigBlinds)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{context} — {position}, {depthInBigBlinds:0.#}bb"),
            string.Create(CultureInfo.CurrentCulture, $"{context} — {position}, {depthInBigBlinds:0.#}bb"));
    }

    public static string OpponentRangeTitle(string position, string street)
    {
        return Language.Pick(
            $"Range assigned to {position} — {street}",
            $"Range attribuée à {position} — {street}");
    }

    public static string CombosSuffix(double combos)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $" · {combos:0.#} combos"),
            string.Create(CultureInfo.CurrentCulture, $" · {combos:0.#} combos"));
    }

    /// <summary>
    /// Pourquoi la case du héros est grise. Postflop la grille montre la range de l'adversaire, pas
    /// la sienne : sans explication, un joueur lit sa propre case éteinte comme une panne.
    /// </summary>
    public static string HeroHandBlocked(HandClass handClass, int combinationCount)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Your {handClass} is greyed out because none of its {combinationCount} combos are left: your cards and the board hold them all."),
            string.Create(CultureInfo.CurrentCulture, $"Ta case {handClass} est éteinte parce qu'il ne reste aucun de ses {combinationCount} combos : tes cartes et le board les occupent tous."));
    }

    public static string HeroHandOutsideRange(HandClass handClass, string position)
    {
        return Language.Pick(
            $"Your {handClass} is greyed out because it is not in the range assigned to {position} — the grid shows his hands, not yours.",
            $"Ta case {handClass} est éteinte parce qu'elle n'est pas dans la range attribuée à {position} — la grille montre ses mains, pas les tiennes.");
    }

    public static string NotYourTurn(string position)
    {
        return Language.Pick(
            $"It is not your turn yet: enter {position}'s action. The advice above is provisional.",
            $"Ce n'est pas encore à toi de parler : saisis l'action de {position}. Le conseil ci-dessus est provisoire.");
    }

    public static string YourTurn(string position)
    {
        return Language.Pick($"Your turn ({position})", $"À toi de parler ({position})");
    }

    public static string TheirTurn(string position)
    {
        return Language.Pick($"{position} to act", $"À {position} de parler");
    }

    public static string BettingRoundOver(string street)
    {
        return Language.Pick(
            $"Betting round closed on the {street.ToLowerInvariant()}.",
            $"Tour d'enchères terminé au {street.ToLowerInvariant()}.");
    }

    public static string Pot(double pot, double potInBigBlinds)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Pot {pot:0.##} chips ({potInBigBlinds:0.#}bb)"),
            string.Create(CultureInfo.CurrentCulture, $"Pot {pot:0.##} jetons ({potInBigBlinds:0.#}bb)"));
    }

    public static string CallAmount(double amount)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Call {amount:0.##}"),
            string.Create(CultureInfo.CurrentCulture, $"Suivre {amount:0.##}"));
    }

    public static string IncompleteBoard(int cardCount)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Incomplete board: {cardCount} card(s) out of 3."),
            string.Create(CultureInfo.CurrentCulture, $"Board incomplet : {cardCount} carte(s) sur 3."));
    }

    public static string CardAlreadyUsed(Card card)
    {
        return Language.Pick(
            $"{card} is already used elsewhere.",
            $"{card} est déjà utilisée ailleurs.");
    }

    public static string PlayerCount(int players)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{players} players"),
            string.Create(CultureInfo.CurrentCulture, $"{players} joueurs"));
    }

    public static string HandToEnter => Language.Pick("hand to enter", "main à saisir");

    public static string DepthLabel(decimal depth)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{depth:0.#}bb deep"),
            string.Create(CultureInfo.CurrentCulture, $"{depth:0.#}bb de profondeur"));
    }

    public static string DepthUnknown => Language.Pick("depth unknown", "profondeur indéterminée");

    public static string RecordedAction(string position, PlayerActionKind kind, double amountTo)
    {
        string what = kind switch
        {
            PlayerActionKind.Fold => Language.Pick("folds", "passe"),
            PlayerActionKind.Check => Language.Pick("checks", "checke"),
            PlayerActionKind.Call => Language.Pick("calls", "suit"),
            PlayerActionKind.Bet => Language.Pick(
                string.Create(CultureInfo.CurrentCulture, $"bets {amountTo:0.##}"),
                string.Create(CultureInfo.CurrentCulture, $"mise à {amountTo:0.##}")),
            _ => Language.Pick(
                string.Create(CultureInfo.CurrentCulture, $"raises to {amountTo:0.##}"),
                string.Create(CultureInfo.CurrentCulture, $"relance à {amountTo:0.##}")),
        };

        return $"{position} {what}";
    }

    public static string JournalCount(int count)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{count} hand(s) recorded."),
            string.Create(CultureInfo.CurrentCulture, $"{count} main(s) enregistrée(s)."));
    }

    public static string ChartsStatus(int chartCount, string directory)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{chartCount} charts · {directory}"),
            string.Create(CultureInfo.CurrentCulture, $"{chartCount} charts · {directory}"));
    }

    public static string ChartsEmbeddedOnly(int chartCount)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{chartCount} bundled charts (editable folder disabled)"),
            string.Create(CultureInfo.CurrentCulture, $"{chartCount} charts livrés (dossier éditable désactivé)"));
    }

    public static string ChartsRestored(int written, int chartCount)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{written} file(s) restored to their original version · {chartCount} charts"),
            string.Create(CultureInfo.CurrentCulture, $"{written} fichier(s) remis à leur version d'origine · {chartCount} charts"));
    }
}
