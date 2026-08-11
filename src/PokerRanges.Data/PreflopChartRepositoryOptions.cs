namespace PokerRanges.Data;

public sealed record PreflopChartRepositoryOptions
{
    /// <summary>
    /// Dossier de charts éditables par l'utilisateur. Un chart qui y porte la même clé qu'un chart
    /// embarqué le remplace ; laisser vide pour n'utiliser que les charts livrés.
    /// </summary>
    public string? UserChartsDirectory { get; init; }

    public static PreflopChartRepositoryOptions EmbeddedOnly { get; } = new();
}
