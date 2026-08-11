using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Preflop;

public static class PreflopContextLabels
{
    public static string Describe(PreflopContext context)
    {
        return context switch
        {
            PreflopContext.RaiseFirstIn => PreflopText.ContextRaiseFirstIn,
            PreflopContext.VersusLimp => PreflopText.ContextVersusLimp,
            PreflopContext.VersusOpen => PreflopText.ContextVersusOpen,
            PreflopContext.Squeeze => PreflopText.ContextSqueeze,
            PreflopContext.VersusThreeBet => PreflopText.ContextVersusThreeBet,
            PreflopContext.VersusFourBet => PreflopText.ContextVersusFourBet,
            PreflopContext.Jam => PreflopText.ContextJam,
            _ => PreflopText.ContextCallJam,
        };
    }

    public static string Describe(FacingRelation relation)
    {
        return relation switch
        {
            FacingRelation.InPosition => PreflopText.RelationInPosition,
            FacingRelation.OutOfPosition => PreflopText.RelationOutOfPosition,
            FacingRelation.SmallBlind => PreflopText.RelationSmallBlind,
            _ => PreflopText.RelationBigBlind,
        };
    }
}
