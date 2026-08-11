namespace PokerRanges.Core.Postflop;

/// <summary>
/// Les paramètres du modèle postflop. Ce ne sont pas des constantes physiques : ce sont des
/// hypothèses, réunies ici pour qu'on puisse les lire, les discuter et les régler, plutôt que
/// dispersées en nombres magiques dans le calcul. Le coût du calcul, lui, n'est pas une hypothèse
/// sur le jeu : il vit dans <see cref="PostflopBudget"/> et se choisit à chaque appel.
/// </summary>
public sealed record PostflopOptions
{
    /// <summary>
    /// Part de son équité qu'un joueur encaisse réellement en checkant. En position on réalise
    /// pratiquement toute son équité ; hors de position on se fait souvent déloger.
    /// </summary>
    public double RealisationInPosition { get; init; } = 1.0;

    public double RealisationOutOfPosition { get; init; } = 0.85;

    /// <summary>
    /// Part du tapis restant qu'un adversaire paie en moyenne quand le tirage rentre. Sert à
    /// valoriser les cotes implicites d'un tirage sans les surestimer.
    /// </summary>
    public double ImpliedOddsFactor { get; init; } = 0.25;

    public IReadOnlyList<double> BetSizesAsPotFraction { get; init; } = [0.33, 0.5, 0.75, 1.0, 1.5];

    /// <summary>Multiple du pot pour la relance envisagée face à une mise.</summary>
    public double RaiseSizeAsPotFraction { get; init; } = 1.0;

    /// <summary>
    /// Écart d'EV en dessous duquel deux actions sont déclarées équivalentes, exprimé en part du
    /// pot. Mieux vaut dire « c'est serré » que trancher sur du bruit de modèle.
    /// </summary>
    public double CloseCallThresholdAsPotFraction { get; init; } = 0.02;

    /// <summary>Graine fixe : à situation identique, le conseil doit être identique.</summary>
    public int RandomSeed { get; init; } = 20260731;

    public static PostflopOptions Default { get; } = new();
}
