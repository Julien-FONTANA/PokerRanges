using PokerRanges.Core.Evaluation;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Table;

namespace PokerRanges.Core.Postflop;

public sealed record PostflopAdvice
{
    public required ActionEvaluation Best { get; init; }

    public required IReadOnlyList<ActionEvaluation> Candidates { get; init; }

    public required HandFeatures HeroHand { get; init; }

    public required BoardTexture Board { get; init; }

    public required IReadOnlyList<OpponentRange> Opponents { get; init; }

    public required PotSnapshot Pot { get; init; }

    public required IReadOnlyList<string> Rationale { get; init; }

    /// <summary>
    /// Vrai quand la deuxième option est à portée de bruit de modèle : mieux vaut le dire que
    /// laisser croire à une décision tranchée.
    /// </summary>
    public required bool IsClose { get; init; }

    public required bool IsHeadsUp { get; init; }

    public required PostflopBudget Budget { get; init; }

    /// <summary>
    /// La pire erreur-type parmi les équités mesurées pour cet avis. Une réponse plus rapide est
    /// une réponse moins précise : autant afficher le prix payé plutôt que de le taire.
    /// </summary>
    public required double EquityStandardError { get; init; }

    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Demi-largeur de l'intervalle à 95 % sur l'équité, convertie en jetons d'espérance sur le
    /// pot en jeu. Ne compte que l'échantillonnage des équités, pas celui du classement de range.
    /// </summary>
    public double ExpectedValueMargin => 1.96 * EquityStandardError * Pot.Pot;

    public string DescribePrecision()
    {
        return PostflopText.Precision(Budget.Name, 1.96 * EquityStandardError, Duration.TotalMilliseconds);
    }
}
