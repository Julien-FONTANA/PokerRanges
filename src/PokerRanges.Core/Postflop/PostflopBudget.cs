using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Postflop;

/// <summary>
/// Le budget de calcul alloué à un conseil. Il est distinct de <see cref="PostflopOptions"/> :
/// celui-ci décrit ce qu'on suppose du jeu, celui-là ce qu'on est prêt à dépenser pour le mesurer.
/// Le mode compact vise une réponse en moins d'une seconde et paie cette vitesse en précision —
/// d'où l'obligation faite à l'avis de dire quelle précision il a réellement atteinte.
/// </summary>
public sealed record PostflopBudget
{
    private readonly Func<string> _name;

    private PostflopBudget(Func<string> name, int rankingSamplesPerCombo, int equitySamples)
    {
        _name = name;
        RankingSamplesPerCombo = rankingSamplesPerCombo;
        EquitySamples = equitySamples;
    }

    public string Name => _name();

    /// <summary>Tirages par combo pour classer une range par force sur le board.</summary>
    public int RankingSamplesPerCombo { get; init; }

    /// <summary>Tirages Monte-Carlo par calcul d'équité.</summary>
    public int EquitySamples { get; init; }

    /// <summary>Budget d'analyse : on prend le temps de la précision.</summary>
    public static PostflopBudget Full { get; } = new(() => PostflopText.BudgetFull, 250, 30_000);

    /// <summary>
    /// Budget du mode compact. Diviser les tirages par quatre multiplie l'erreur-type par deux
    /// seulement — la racine carrée joue en notre faveur — ce qui rend l'échange rentable quand on
    /// est à la table et qu'on attend la réponse.
    /// </summary>
    public static PostflopBudget Fast { get; } = new(() => PostflopText.BudgetFast, 60, 5_000);

    public string Describe()
    {
        return PostflopText.BudgetSummary(Name, EquitySamples, RankingSamplesPerCombo);
    }
}
