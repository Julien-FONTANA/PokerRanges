using System.Globalization;

namespace PokerRanges.Core.Localization;

/// <summary>
/// The current language. It is not held in a field of its own: it <em>is</em>
/// <see cref="CultureInfo.CurrentUICulture"/>.
/// <para>
/// Two reasons for that. First, numbers then follow the language without anyone having to think
/// about it — "5,5bb" in French, "5.5bb" in English — because the formatting culture changes with
/// it. Second, the culture travels with the execution context: a computation running on a pool
/// thread or behind an <c>await</c> renders its sentences in the right language, and two parallel
/// tests choosing different languages do not tread on each other.
/// </para>
/// </summary>
public static class Language
{
    private const string FrenchCultureName = "fr-FR";
    private const string EnglishCultureName = "en-US";

    /// <summary>Raised after every change, so the display can rebuild itself.</summary>
    public static event EventHandler? Changed;

    public static AppLanguage Current => IsFrench ? AppLanguage.French : AppLanguage.English;

    public static bool IsFrench => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
        .Equals("fr", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Switches the whole application, background threads included. Reserved for the application:
    /// a test that only needs its own language sets <see cref="CultureInfo.CurrentUICulture"/> and
    /// leaves the rest of the process alone.
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

    /// <summary>Picks between two already-written phrasings.</summary>
    public static string Pick(string english, string french)
    {
        return IsFrench ? french : english;
    }
}
