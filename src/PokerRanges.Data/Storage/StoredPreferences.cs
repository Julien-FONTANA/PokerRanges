using PokerRanges.Core.Localization;
using PokerRanges.Core.Table;

namespace PokerRanges.Data.Storage;

public sealed class StoredPreferences
{
    public int PlayerCount { get; set; } = 8;

    public double BigBlind { get; set; } = 8;

    public double StartingStack { get; set; } = 1000;

    public AnteStyle AnteStyle { get; set; } = AnteStyle.None;

    public double AnteAmount { get; set; } = 8;

    public Position HeroPosition { get; set; } = Position.Button;

    public string OpponentProfile { get; set; } = "Balanced";

    public bool PrefersCompactLayout { get; set; }

    public AppLanguage Language { get; set; } = AppLanguage.English;
}
