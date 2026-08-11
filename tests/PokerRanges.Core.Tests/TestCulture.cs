using System.Runtime.CompilerServices;
using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Tests;

/// <summary>
/// Fixe l'anglais avant le premier test. Sans cela les assertions sur le texte passeraient sur une
/// machine française et échoueraient ailleurs : la langue serait celle du poste, pas celle du
/// produit. Un test qui veut du français pose <see cref="System.Globalization.CultureInfo"/> sur
/// son propre contexte, qui ne déborde pas sur les autres.
/// </summary>
internal static class TestCulture
{
    [ModuleInitializer]
    internal static void UseEnglish()
    {
        Language.Use(AppLanguage.English);
    }
}
