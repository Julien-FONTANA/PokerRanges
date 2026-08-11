using PokerRanges.Core.Table;

namespace PokerRanges.Core.Session;

/// <summary>
/// Remembers the settings and the hand in progress from one run to the next. Reads never throw:
/// an unreadable file counts as "nothing saved", because an assistant that refuses to start
/// because of its own resume file is worse than one that has forgotten everything.
/// </summary>
public interface ISessionStore
{
    UserPreferences LoadPreferences();

    void SavePreferences(UserPreferences preferences);

    /// <summary>The hand interrupted at the last shutdown, or null if there is none.</summary>
    HandState? LoadHand();

    /// <summary>Passing null clears the resume file: the hand is over, nothing left to resume.</summary>
    void SaveHand(HandState? hand);
}
