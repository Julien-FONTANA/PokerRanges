using System.Runtime.CompilerServices;
using PokerRanges.Core.Localization;

namespace PokerRanges.App.Tests;

/// <summary>
/// Fixe l'anglais avant le premier test. Sans cela les assertions sur le texte passeraient sur une
/// machine franÃ§aise et Ã©choueraient ailleurs : la langue serait celle du poste, pas celle du
/// produit. Un test qui veut du franÃ§ais pose <see cref="System.Globalization.CultureInfo"/> sur
/// son propre contexte, qui ne dÃ©borde pas sur les autres.
/// </summary>
internal static class TestCulture
{
    [ModuleInitializer]
    internal static void UseEnglish()
    {
        Language.Use(AppLanguage.English);
    }
}

