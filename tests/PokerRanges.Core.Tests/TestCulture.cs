using System.Runtime.CompilerServices;
using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Tests;

/// <summary>
/// Pins English before the first test. Without it, assertions on text would pass on a French
/// machine and fail elsewhere: the language would be the workstation's, not the product's. A test
/// that wants French sets <see cref="System.Globalization.CultureInfo"/> on its own context,
/// which does not spill onto the others.
/// </summary>
internal static class TestCulture
{
    [ModuleInitializer]
    internal static void UseEnglish()
    {
        Language.Use(AppLanguage.English);
    }
}
