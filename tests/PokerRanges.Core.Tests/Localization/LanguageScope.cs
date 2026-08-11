using System.Globalization;
using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Tests.Localization;

/// <summary>
/// Parle une langue le temps d'un bloc. La culture n'est posée que sur le contexte courant, jamais
/// sur les valeurs par défaut du processus : deux tests parallèles peuvent ainsi tenir deux langues
/// différentes sans se contredire.
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
