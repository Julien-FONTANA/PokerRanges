using System.Globalization;
using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Tests.Localization;

/// <summary>
/// Speaks a language for the duration of a block. The culture is set on the current context only,
/// never on the process defaults: two parallel tests can therefore hold two different languages
/// without contradicting each other.
/// </summary>
internal sealed class LanguageScope : IDisposable
{
    private readonly CultureInfo _previousCulture;
    private readonly CultureInfo _previousUiCulture;

    public LanguageScope(AppLanguage language)
    {
        _previousCulture = CultureInfo.CurrentCulture;
        _previousUiCulture = CultureInfo.CurrentUICulture;

        CultureInfo culture = Language.CultureOf(language);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _previousCulture;
        CultureInfo.CurrentUICulture = _previousUiCulture;
    }
}
