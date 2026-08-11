using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Preflop;

/// <summary>
/// Une action d'un chart et la range qui la joue, en notation standard. Les poids partiels
/// (« AKo:0.5 ») expriment les stratégies mixtes ; ce qui n'est listé nulle part est un fold.
/// </summary>
public sealed record ChartAction
{
    public required ChartActionKind Kind { get; init; }

    public double SizeInBigBlinds { get; init; }

    public required string Range { get; init; }
}

/// <summary>
/// Un chart préflop. Il n'est pas indexé par un libellé de position mais par le nombre de joueurs
/// qui parlent après le héros : ouvrir avec trois joueurs derrière pose le même problème à une
/// table de cinq et à une table de huit, ce qui divise d'autant le volume de données à écrire.
/// <para>
/// Exception connue : la petite blinde n'a qu'un joueur derrière mais parle la première à chaque
/// tour postflop. Sa range est donc plus serrée que celle du bouton, à rebours de la tendance
/// générale — c'est une donnée à écrire, pas une anomalie à corriger.
/// </para>
/// </summary>
public sealed record PreflopChart
{
    public required PreflopContext Context { get; init; }

    public required int PlayersLeftToAct { get; init; }

    public FacingRelation? Relation { get; init; }

    public required double DepthInBigBlinds { get; init; }

    public required IReadOnlyList<ChartAction> Actions { get; init; }

    /// <summary>D'où vient cette range : à afficher pour que le conseil reste auditable.</summary>
    public string Source { get; init; } = string.Empty;

    public string Describe()
    {
        string relation = Relation is null ? string.Empty : $" {PreflopContextLabels.Describe(Relation.Value)},";

        return PreflopText.ChartSummary(
            PreflopContextLabels.Describe(Context),
            relation,
            PlayersLeftToAct,
            DepthInBigBlinds);
    }
}
