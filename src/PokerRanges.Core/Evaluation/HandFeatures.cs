using PokerRanges.Core.Cards;
using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Evaluation;

/// <summary>
/// Tout ce qu'on sait de la main du héros sur ce board : sa force actuelle, ses tirages, et de
/// combien de cartes elle peut encore s'améliorer. C'est la matière première des explications.
/// </summary>
public sealed record HandFeatures
{
    public required HandValue Value { get; init; }

    public required MadeHandTier Tier { get; init; }

    public required bool IsNuts { get; init; }

    /// <summary>Cartes qui font passer la main à deux paires ou mieux au prochain tirage.</summary>
    public required int Outs { get; init; }

    public required int StraightOuts { get; init; }

    public required bool HasFlushDraw { get; init; }

    public required bool HasOpenEndedStraightDraw { get; init; }

    public required bool HasGutshot { get; init; }

    public required Rank? PairedRank { get; init; }

    public bool HasDraw => HasFlushDraw || HasOpenEndedStraightDraw || HasGutshot;

    public bool IsComboDraw => HasFlushDraw && (HasOpenEndedStraightDraw || HasGutshot);

    public bool IsStrongMadeHand => Tier >= MadeHandTier.TwoPair;

    /// <summary>
    /// Probabilité d'améliorer d'ici la river, formule classique des outs : environ 4 % par out
    /// depuis le flop, 2 % depuis la turn.
    /// </summary>
    public double ImprovementChance(int boardCardCount)
    {
        if (Outs == 0 || boardCardCount >= 5)
        {
            return 0;
        }

        int unseen = 52 - boardCardCount - 2;
        int draws = 5 - boardCardCount;

        double missChance = 1;
        for (int step = 0; step < draws; step++)
        {
            missChance *= (double)(unseen - Outs - step) / (unseen - step);
        }

        return 1 - missChance;
    }

    public string Describe()
    {
        List<string> parts = [DescribeTier()];

        if (HasFlushDraw)
        {
            parts.Add(HandText.FlushDraw);
        }

        if (HasOpenEndedStraightDraw)
        {
            parts.Add(HandText.OpenEndedDraw);
        }
        else if (HasGutshot)
        {
            parts.Add(HandText.Gutshot);
        }

        if (IsNuts)
        {
            parts.Add(HandText.Nuts);
        }

        return string.Join(", ", parts);
    }

    private string DescribeTier()
    {
        return Tier switch
        {
            MadeHandTier.HighCard => HandText.TierHighCard,
            MadeHandTier.UnderPair => HandText.TierUnderPair,
            MadeHandTier.BottomPair => HandText.TierBottomPair,
            MadeHandTier.MiddlePair => HandText.TierMiddlePair,
            MadeHandTier.TopPair => HandText.TierTopPair,
            MadeHandTier.Overpair => HandText.TierOverpair,
            MadeHandTier.TwoPair => HandText.TierTwoPair,
            MadeHandTier.Trips => HandText.TierTrips,
            MadeHandTier.Set => HandText.TierSet,
            MadeHandTier.Straight => HandText.TierStraight,
            MadeHandTier.Flush => HandText.TierFlush,
            MadeHandTier.FullHouse => HandText.TierFullHouse,
            MadeHandTier.Quads => HandText.TierQuads,
            _ => HandText.TierStraightFlush,
        };
    }
}
