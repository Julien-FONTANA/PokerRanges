using PokerRanges.Core.Localization;
using PokerRanges.Core.Table;

namespace PokerRanges.Core.HeadToHead;

/// <summary>
/// What is at stake in a two-player all-in, in chips. Everything is counted against the effective
/// stack rather than the raw pot: a jam larger than the caller's stack has its excess handed back,
/// and counting that excess would overstate what is actually contested — which is exactly how a
/// short caller's price gets misread.
/// </summary>
public sealed record HeadToHeadSpot
{
    /// <summary>Chips left behind by players no longer in the hand: their antes and their blinds.</summary>
    public required double DeadChips { get; init; }

    /// <summary>
    /// The contested stack: the smaller of the two starting stacks, blinds and antes included. It is
    /// this figure, and not either player's remaining stack, that sizes the showdown.
    /// </summary>
    public required double EffectiveStack { get; init; }

    /// <summary>The hero's own chips already in the middle.</summary>
    public required double HeroCommitted { get; init; }

    /// <summary>The villain's chips already in the middle, before any jam.</summary>
    public required double VillainCommitted { get; init; }

    public required HeadToHeadRole Role { get; init; }

    /// <summary>Carried so the expectation can be quoted in big blinds, the unit of a final table.</summary>
    public required double BigBlind { get; init; }

    /// <summary>What is actually won at showdown: the dead money plus both contested stacks.</summary>
    public double ContestedPot => DeadChips + (2 * EffectiveStack);

    public double DepthInBigBlinds => BigBlind <= 0 ? 0 : EffectiveStack / BigBlind;

    /// <summary>What the hero still has to put in — the jam, or the amount needed to call one.</summary>
    public double HeroRisk => Math.Max(0, EffectiveStack - HeroCommitted);

    /// <summary>
    /// What the villain still has to put in to contest the pot. Not part of the expectation —
    /// <see cref="ContestedPot"/> already accounts for it — but zero means an opponent who has no
    /// fold left in them.
    /// </summary>
    public double VillainRisk => Math.Max(0, EffectiveStack - VillainCommitted);

    /// <summary>
    /// What a fold by the villain wins: the pot as it stands, the hero's own posted chips included,
    /// since an uncalled jam comes straight back.
    /// </summary>
    public double UncontestedPot => DeadChips + HeroCommitted + VillainCommitted;

    /// <summary>
    /// The equity needed to break even <em>once called</em>. On a jam this is not the threshold to
    /// beat — fold equity subsidises that one; see
    /// <see cref="HeadToHeadResult.BreakEvenFoldFrequency"/>.
    /// </summary>
    public double BreakEvenEquityIfCalled => ContestedPot <= 0 ? 0 : HeroRisk / ContestedPot;

    /// <summary>
    /// The hero's chips are already in and there is no decision left. Reachable for real: a big
    /// blind shorter than its own ante posts the ante and no blind at all.
    /// </summary>
    public bool HeroIsAllIn => HeroRisk <= 0;

    /// <summary>
    /// The villain has nothing left to put in, so there is no fold equity to collect against them.
    /// </summary>
    public bool VillainIsAllIn => VillainRisk <= 0;

    /// <summary>
    /// Reads the spot off a table, so that the blind and ante rules — in particular a big blind ante
    /// counting as dead money rather than towards the current bet — are taken from
    /// <see cref="IPotEngine"/> rather than restated here. Everyone but the two contestants is
    /// folded, which leaves their blinds and every ante in the pot as dead money.
    /// </summary>
    public static HeadToHeadSpot BetweenSeats(
        IPotEngine potEngine,
        TableConfiguration table,
        Position villainSeat,
        HeadToHeadRole role)
    {
        ArgumentNullException.ThrowIfNull(potEngine);
        ArgumentNullException.ThrowIfNull(table);
        table.Validate();

        if (!PositionLayout.IsSeated(table.PlayerCount, villainSeat))
        {
            throw new HeadToHeadException(HeadToHeadText.VillainNotSeated(table.PlayerCount));
        }

        if (villainSeat == table.HeroPosition)
        {
            throw new HeadToHeadException(HeadToHeadText.VillainMustBeAnotherSeat);
        }

        List<PlayerAction> folds = [];
        foreach (Position seat in PositionLayout.Seats(table.PlayerCount))
        {
            if (seat != table.HeroPosition && seat != villainSeat)
            {
                folds.Add(PlayerAction.Fold(Street.Preflop, seat));
            }
        }

        HandAnalysis analysis = potEngine.Analyse(new HandState { Table = table, Actions = folds });

        if (analysis.LiveOpponents.Count != 1)
        {
            throw new HeadToHeadException(HeadToHeadText.OpponentMustBeAlone(analysis.LiveOpponents.Count));
        }

        PotSnapshot hero = analysis.Hero;
        PotSnapshot villain = analysis.For(villainSeat);

        if (hero.EffectiveStartingStack <= 0)
        {
            throw new HeadToHeadException(HeadToHeadText.NoEffectiveStack);
        }

        return new HeadToHeadSpot
        {
            DeadChips = analysis.Pot - hero.Committed - villain.Committed,
            EffectiveStack = hero.EffectiveStartingStack,
            HeroCommitted = hero.Committed,
            VillainCommitted = villain.Committed,
            Role = role,
            BigBlind = table.BigBlind,
        };
    }
}
