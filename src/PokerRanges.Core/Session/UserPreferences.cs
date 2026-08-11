using PokerRanges.Core.Localization;
using PokerRanges.Core.Table;

namespace PokerRanges.Core.Session;

/// <summary>
/// Ce que l'utilisateur ne veut pas ressaisir à chaque lancement : sa table habituelle et la façon
/// dont il regarde l'application. Volontairement pauvre — tout ce qui appartient à une main donnée
/// vit dans <see cref="HandState"/>, pas ici.
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
    /// Le nom du profil dans la langue où il a été enregistré. Il est relu de façon tolérante :
    /// changer de langue ne doit pas faire retomber l'utilisateur sur le profil par défaut.
    /// </summary>
    public string OpponentProfile { get; init; } = "Balanced";

    public bool PrefersCompactLayout { get; init; }

    public AppLanguage Language { get; init; } = AppLanguage.English;

    public static UserPreferences Default { get; } = new();
}
