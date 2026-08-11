namespace PokerRanges.Core.Table;

public enum AnteStyle
{
    None,

    /// <summary>Chaque joueur assis paie l'ante.</summary>
    PerPlayer,

    /// <summary>La grosse blinde paie l'ante pour toute la table, structure devenue standard en tournoi.</summary>
    BigBlindAnte,
}
