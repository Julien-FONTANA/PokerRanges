using System.Globalization;

namespace PokerRanges.Core.Localization;

public static class SessionText
{
    public static string UnknownHand => Language.Pick("hand unknown", "main inconnue");

    public static string BeforeTheFlop => Language.Pick("preflop", "préflop");

    public static string JournalHand(string position, string hero, string board, int playerCount)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{position} {hero} · {board} · {playerCount} players"),
            string.Create(CultureInfo.CurrentCulture, $"{position} {hero} · {board} · {playerCount} joueurs"));
    }

    public static string StoredHandUnreadable(string text)
    {
        return Language.Pick(
            $"The stored hand « {text} » cannot be read.",
            $"La main enregistrée « {text} » n'est pas lisible.");
    }

    public static string StoredBoardUnreadable(string text, string reason)
    {
        return Language.Pick(
            $"The stored board « {text} » cannot be read: {reason}",
            $"Le board enregistré « {text} » n'est pas lisible : {reason}");
    }
}
