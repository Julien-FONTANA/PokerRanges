namespace PokerRanges.Core.Table;

/// <summary>
/// What a given player is looking at at this point in the hand: what is in the pot, what they owe,
/// what they have left, and the ratios that follow from it.
/// </summary>
public sealed record PotSnapshot
{
    public required Position Position { get; init; }

    public required double Pot { get; init; }

    public required double AmountToCall { get; init; }

    /// <summary>Everything this player has committed since the start of the hand, antes included.</summary>
    public required double Committed { get; init; }

    /// <summary>What they have committed on the current street; antes are not part of it.</summary>
    public required double StreetCommitted { get; init; }

    public required double RemainingStack { get; init; }

    /// <summary>The most this player can still commit against at least one opponent.</summary>
    public required double EffectiveStack { get; init; }

    /// <summary>
    /// The depth of the hand: the effective stack including blinds and antes, before the hand
    /// starts. It is this value, not the remaining stack, that indexes the charts.
    /// </summary>
    public required double EffectiveStartingStack { get; init; }

    public required double BigBlind { get; init; }

    public bool IsFacingABet => AmountToCall > 0;

    /// <summary>Minimum equity needed for calling to be profitable.</summary>
    public double RequiredEquityToCall => AmountToCall <= 0 ? 0 : AmountToCall / (Pot + AmountToCall);

    /// <summary>
    /// The share of their range a player must defend so that an opponent's bluff is not
    /// automatically profitable. Equals 1 when there is nothing to call.
    /// </summary>
    public double MinimumDefenceFrequency => AmountToCall <= 0 || Pot <= 0
        ? 1
        : Math.Clamp((Pot - AmountToCall) / Pot, 0, 1);

    public double StackToPotRatio => Pot <= 0 ? 0 : EffectiveStack / Pot;

    public double PotInBigBlinds => Pot / BigBlind;

    public double AmountToCallInBigBlinds => AmountToCall / BigBlind;

    public double EffectiveStackInBigBlinds => EffectiveStack / BigBlind;

    /// <summary>The maximum total this player can raise to on this street, all-in included.</summary>
    public double MaximumRaiseTo => StreetCommitted + RemainingStack;

    public double DepthInBigBlinds => EffectiveStartingStack / BigBlind;
}
