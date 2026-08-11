using PokerRanges.Core.Localization;
using PokerRanges.Core.Table;

namespace PokerRanges.Core.Session;

/// <summary>
/// What the user does not want to re-enter on every launch: their usual table and how they look
/// at the application. Deliberately thin — anything belonging to a particular hand lives in
/// <see cref="HandState"/>, not here.
/// </summary>
public sealed record UserPreferences
{
    public int PlayerCount { get; init; } = 8;

    public double BigBlind { get; init; } = 8;

    public double StartingStack { get; init; } = 1000;

    public AnteStyle AnteStyle { get; init; } = AnteStyle.None;

    public double AnteAmount { get; init; } = 8;

    public Position HeroPosition { get; init; } = Position.Button;

    /// <summary>
    /// The profile name in whichever language it was saved. It is read back leniently: switching
    /// language must not drop the user back onto the default profile.
    /// </summary>
    public string OpponentProfile { get; init; } = "Balanced";

    public bool PrefersCompactLayout { get; init; }

    public AppLanguage Language { get; init; } = AppLanguage.English;

    public static UserPreferences Default { get; } = new();
}
