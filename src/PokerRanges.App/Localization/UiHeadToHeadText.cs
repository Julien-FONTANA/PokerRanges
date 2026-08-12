using System.Globalization;
using PokerRanges.Core.Localization;

namespace PokerRanges.App.Localization;

/// <summary>
/// The head-to-head sentences that carry a value. Fixed labels live on <see cref="UiText"/>; these
/// are composed, so they are rebuilt rather than merely re-read when the language changes.
/// </summary>
public static class UiHeadToHeadText
{
    public static string RangeSummary(double combos, double percentOfAllHands)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{combos:0.#} combos · {percentOfAllHands:0.#}% of all hands"),
            string.Create(CultureInfo.CurrentCulture, $"{combos:0.#} combos · {percentOfAllHands:0.#}% de toutes les mains"));
    }

    public static string EquityHeadline(double heroEquity, double villainEquity)
    {
        return string.Create(CultureInfo.CurrentCulture, $"{heroEquity:P1} — {villainEquity:P1}");
    }

    public static string WinTieLose(double win, double tie, double lose)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"Wins {win:P1} · ties {tie:P1} · loses {lose:P1}"),
            string.Create(CultureInfo.CurrentCulture, $"Gagne {win:P1} · partage {tie:P1} · perd {lose:P1}"));
    }

    public static string Chips(double amount, double inBigBlinds)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{amount:0.##} chips ({inBigBlinds:0.#}bb)"),
            string.Create(CultureInfo.CurrentCulture, $"{amount:0.##} jetons ({inBigBlinds:0.#}bb)"));
    }

    public static string ExpectedValue(double inBigBlinds)
    {
        return string.Create(CultureInfo.CurrentCulture, $"{inBigBlinds:+0.0;-0.0;0.0}bb");
    }

    public static string Percent(double value)
    {
        return string.Create(CultureInfo.CurrentCulture, $"{value:P1}");
    }

    public static string DepthLabel(double depthInBigBlinds)
    {
        return string.Create(CultureInfo.CurrentCulture, $"{depthInBigBlinds:0.#}bb");
    }

    public static string NotApplicable => Language.Pick("not needed", "sans objet");
}
