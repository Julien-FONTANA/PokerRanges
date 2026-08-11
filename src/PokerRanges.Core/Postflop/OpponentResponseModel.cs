using PokerRanges.Core.Ranges;

namespace PokerRanges.Core.Postflop;

/// <summary>
/// Splits a strength-ranked range into reactions to a bet. The cut-off starts from the minimum
/// defence frequency — the one that makes an opponent's bluff just barely unprofitable — then
/// shifts with the profile: a tight player folds below it, a calling station calls above it.
/// </summary>
public static class OpponentResponseModel
{
    public static RangeSplit SplitFacingBet(
        IReadOnlyList<RankedCombo> ranked,
        double potBeforeBet,
        double bet,
        OpponentProfile profile)
    {
        ArgumentNullException.ThrowIfNull(ranked);
        ArgumentNullException.ThrowIfNull(profile);

        double total = TotalWeightOf(ranked);
        if (total <= 0)
        {
            return RangeSplit.Empty;
        }

        double minimumDefence = bet <= 0 ? 1 : potBeforeBet / (potBeforeBet + bet);
        double continueFraction = Math.Clamp(minimumDefence * profile.DefenceFactor, 0, 1);

        double raiseBudget = total * continueFraction * profile.RaiseFraction;
        double callBudget = total * continueFraction * (1 - profile.RaiseFraction);

        HandRangeBuilder raising = new();
        HandRangeBuilder calling = new();
        HandRangeBuilder folding = new();
        HandRangeBuilder continuing = new();

        double raised = 0;
        double called = 0;
        double folded = 0;

        foreach (RankedCombo entry in ranked)
        {
            double remaining = entry.Weight;

            double toRaise = Math.Min(remaining, raiseBudget - raised);
            if (toRaise > 0)
            {
                raising.Set(entry.Combo, toRaise);
                continuing.Set(entry.Combo, toRaise);
                raised += toRaise;
                remaining -= toRaise;
            }

            double toCall = Math.Min(remaining, callBudget - called);
            if (toCall > 0)
            {
                calling.Set(entry.Combo, toCall);
                continuing.Set(entry.Combo, continuing.GetWeight(entry.Combo) + toCall);
                called += toCall;
                remaining -= toCall;
            }

            if (remaining > 0)
            {
                folding.Set(entry.Combo, remaining);
                folded += remaining;
            }
        }

        return new RangeSplit(
            folding.Build(),
            calling.Build(),
            raising.Build(),
            continuing.Build(),
            folded / total,
            called / total,
            raised / total);
    }

    /// <summary>
    /// The range an opponent bets of their own accord: their best hands for value, and a tail of
    /// weak ones for bluffs. A polarised range, as in real life.
    /// </summary>
    public static HandRange BettingRange(IReadOnlyList<RankedCombo> ranked, OpponentProfile profile)
    {
        ArgumentNullException.ThrowIfNull(ranked);
        ArgumentNullException.ThrowIfNull(profile);

        double total = TotalWeightOf(ranked);
        if (total <= 0)
        {
            return HandRange.Empty;
        }

        HandRangeBuilder betting = new();

        double valueBudget = total * profile.BettingFraction;
        double taken = 0;

        foreach (RankedCombo entry in ranked)
        {
            double toTake = Math.Min(entry.Weight, valueBudget - taken);
            if (toTake <= 0)
            {
                break;
            }

            betting.Set(entry.Combo, toTake);
            taken += toTake;
        }

        double bluffBudget = total * profile.BluffFraction;
        double bluffed = 0;

        for (int index = ranked.Count - 1; index >= 0 && bluffed < bluffBudget; index--)
        {
            RankedCombo entry = ranked[index];
            double free = entry.Weight - betting.GetWeight(entry.Combo);
            double toTake = Math.Min(free, bluffBudget - bluffed);

            if (toTake <= 0)
            {
                continue;
            }

            betting.Set(entry.Combo, betting.GetWeight(entry.Combo) + toTake);
            bluffed += toTake;
        }

        return betting.Build();
    }

    private static double TotalWeightOf(IReadOnlyList<RankedCombo> ranked)
    {
        double total = 0;
        foreach (RankedCombo entry in ranked)
        {
            total += entry.Weight;
        }

        return total;
    }
}
