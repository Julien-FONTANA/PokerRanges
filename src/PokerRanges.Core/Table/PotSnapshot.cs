namespace PokerRanges.Core.Table;

/// <summary>
/// Ce qu'un joueur donné a sous les yeux à cet instant de la main : ce qu'il y a dans le pot, ce
/// qu'il doit payer, ce qu'il lui reste, et les ratios qui en découlent.
/// </summary>
public sealed record PotSnapshot
{
    public required Position Position { get; init; }

    public required double Pot { get; init; }

    public required double AmountToCall { get; init; }

    /// <summary>Tout ce que ce joueur a engagé depuis le début de la main, antes comprises.</summary>
    public required double Committed { get; init; }

    /// <summary>Ce qu'il a engagé sur la street en cours ; les antes n'en font pas partie.</summary>
    public required double StreetCommitted { get; init; }

    public required double RemainingStack { get; init; }

    /// <summary>Le maximum que ce joueur peut encore engager face à au moins un adversaire.</summary>
    public required double EffectiveStack { get; init; }

    /// <summary>
    /// La profondeur du coup : le tapis effectif blindes et antes comprises, avant que la main ne
    /// commence. C'est cette valeur, et non le tapis restant, qui indexe les charts.
    /// </summary>
    public required double EffectiveStartingStack { get; init; }

    public required double BigBlind { get; init; }

    public bool IsFacingABet => AmountToCall > 0;

    /// <summary>Équité minimale nécessaire pour que payer soit rentable.</summary>
    public double RequiredEquityToCall => AmountToCall <= 0 ? 0 : AmountToCall / (Pot + AmountToCall);

    /// <summary>
    /// Part de sa range qu'un joueur doit défendre pour qu'un bluff adverse ne soit pas
    /// automatiquement rentable. Vaut 1 quand il n'y a rien à payer.
    /// </summary>
    public double MinimumDefenceFrequency => AmountToCall <= 0 || Pot <= 0
        ? 1
        : Math.Clamp((Pot - AmountToCall) / Pot, 0, 1);

    public double StackToPotRatio => Pot <= 0 ? 0 : EffectiveStack / Pot;

    public double PotInBigBlinds => Pot / BigBlind;

    public double AmountToCallInBigBlinds => AmountToCall / BigBlind;

    public double EffectiveStackInBigBlinds => EffectiveStack / BigBlind;

    /// <summary>Le total maximum auquel ce joueur peut relancer sur cette street, tapis compris.</summary>
    public double MaximumRaiseTo => StreetCommitted + RemainingStack;

    public double DepthInBigBlinds => EffectiveStartingStack / BigBlind;
}
