using PokerRanges.Core.Cards;
using PokerRanges.Core.Ranges;

namespace PokerRanges.Core.Equity;

/// <summary>
/// Une demande de calcul d'équité. Par convention le joueur d'indice 0 est le héros :
/// c'est son équité qui pilote le critère de convergence.
/// </summary>
public sealed record EquityRequest
{
    public required IReadOnlyList<HandRange> PlayerRanges { get; init; }

    public IReadOnlyList<Card> Board { get; init; } = [];

    public IReadOnlyList<Card> DeadCards { get; init; } = [];

    public EquityMethod Method { get; init; } = EquityMethod.Automatic;

    /// <summary>Plafond d'échantillons en Monte-Carlo. Sans effet sur l'énumération exhaustive.</summary>
    public int MaximumSamples { get; init; } = 200_000;

    /// <summary>
    /// Erreur-type visée sur l'équité du héros ; le tirage s'arrête dès qu'elle est atteinte.
    /// 0,0015 correspond à ± 0,15 point d'équité environ.
    /// </summary>
    public double TargetStandardError { get; init; } = 0.0015;

    /// <summary>
    /// Graine fixe : rend le tirage reproductible en forçant un seul fil d'exécution.
    /// Réservé aux tests et au diagnostic.
    /// </summary>
    public int? RandomSeed { get; init; }

    public static EquityRequest Between(params HandRange[] playerRanges)
    {
        return new EquityRequest { PlayerRanges = playerRanges };
    }
}
