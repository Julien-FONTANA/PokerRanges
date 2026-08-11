namespace PokerRanges.Core.Postflop;

/// <summary>
/// Une action envisagée et son espérance de gain, en jetons, comptée à partir de maintenant :
/// passer vaut zéro, ce qui est déjà dans le pot est perdu de toute façon.
/// </summary>
public sealed record ActionEvaluation
{
    public required PostflopActionKind Kind { get; init; }

    /// <summary>Jetons supplémentaires engagés par cette action.</summary>
    public required double Amount { get; init; }

    public required double ExpectedValue { get; init; }

    /// <summary>Équité du héros contre la range qui continue face à cette action.</summary>
    public required double Equity { get; init; }

    /// <summary>
    /// Erreur-type sur <see cref="Equity"/>. Zéro quand aucun tirage n'a été nécessaire — passer
    /// vaut zéro sans le moindre calcul.
    /// </summary>
    public double EquityStandardError { get; init; }

    public required double FoldProbability { get; init; }

    public required string Label { get; init; }

    public required string Explanation { get; init; }

    public double ExpectedValueInBigBlinds(double bigBlind)
    {
        return bigBlind <= 0 ? 0 : ExpectedValue / bigBlind;
    }
}
