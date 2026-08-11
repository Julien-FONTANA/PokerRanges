using System.Globalization;

namespace PokerRanges.Core.Localization;

/// <summary>
/// La langue courante. Elle n'est pas stockée dans un champ à part : elle <em>est</em>
/// <see cref="CultureInfo.CurrentUICulture"/>.
/// <para>
/// Deux raisons à cela. D'abord les nombres suivent alors la langue sans qu'on ait à y penser —
/// « 5,5bb » en français, « 5.5bb » en anglais — car la culture de formatage change en même temps.
/// Ensuite la culture circule avec le contexte d'exécution : un calcul parti sur un fil du pool ou
/// derrière un <c>await</c> rend ses phrases dans la bonne langue, et deux tests parallèles qui
/// choisissent des langues différentes ne se marchent pas dessus.
/// </para>
/// </summary>
public static class Language
{
    private const string FrenchCultureName = "fr-FR";
    private const string EnglishCultureName = "en-US";

    /// <summary>Prévenu après chaque changement, pour que l'affichage se reconstruise.</summary>
    public static event EventHandler? Changed;

    public static AppLanguage Current => IsFrench ? AppLanguage.French : AppLanguage.English;

    public static bool IsFrench => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
        .Equals("fr", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Bascule l'application entière, fils d'arrière-plan compris. Réservé à l'application : un
    /// test qui n'a besoin que de sa propre langue pose <see cref="CultureInfo.CurrentUICulture"/>
    /// et laisse le reste du processus tranquille.
    /// </summary>
    public static void Use(AppLanguage language)
    {
        CultureInfo culture = CultureOf(language);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static CultureInfo CultureOf(AppLanguage language)
    {
        return CultureInfo.GetCultureInfo(language == AppLanguage.French ? FrenchCultureName : EnglishCultureName);
    }

    /// <summary>Choisit entre deux formulations déjà construites.</summary>
    public static string Pick(string english, string french)
    {
        return IsFrench ? french : english;
    }
}
