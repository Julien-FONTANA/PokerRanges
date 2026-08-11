namespace PokerRanges.Core.Postflop;

/// <summary>
/// The parameters of the postflop model. These are not physical constants: they are assumptions,
/// gathered here so they can be read, argued with and tuned, rather than scattered as magic numbers
/// through the calculation. The cost of the calculation is not an assumption about the game: it
/// lives in <see cref="PostflopBudget"/> and is chosen per call.
/// </summary>
public sealed record PostflopOptions
{
    /// <summary>
    /// The share of its equity a hand actually collects by checking. In position you realise
    /// nearly all of it; out of position you are often bet off the hand.
    /// </summary>
    public double RealisationInPosition { get; init; } = 1.0;

    public double RealisationOutOfPosition { get; init; } = 0.85;

    /// <summary>
    /// The share of the remaining stack an opponent pays off on average when the draw comes in.
    /// Used to value a draw's implied odds without overstating them.
    /// </summary>
    public double ImpliedOddsFactor { get; init; } = 0.25;

    public IReadOnlyList<double> BetSizesAsPotFraction { get; init; } = [0.33, 0.5, 0.75, 1.0, 1.5];

    /// <summary>Pot multiple for the raise considered when facing a bet.</summary>
    public double RaiseSizeAsPotFraction { get; init; } = 1.0;

    /// <summary>
    /// The EV gap below which two actions are declared equivalent, as a fraction of the pot.
    /// Better to say "it is close" than to decide on model noise.
    /// </summary>
    public double CloseCallThresholdAsPotFraction { get; init; } = 0.02;

    /// <summary>Fixed seed: the same situation must always produce the same advice.</summary>
    public int RandomSeed { get; init; } = 20260731;

    public static PostflopOptions Default { get; } = new();
}
