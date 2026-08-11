using PokerRanges.Core.Table;

namespace PokerRanges.Core.Preflop;

/// <summary>
/// Turns the action history into a chart situation: who opened, how many times it was raised,
/// where the hero sits relative to the aggressor, and at what depth the hand is being played.
/// </summary>
public static class PreflopSituationReader
{
    public static PreflopSituation Read(HandState state, HandAnalysis analysis, double jamThresholdInBigBlinds)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(analysis);

        if (analysis.Street != Street.Preflop)
        {
            throw new PreflopChartException(
                $"La lecture de situation préflop a été demandée alors que la main en est au {analysis.Street}.");
        }

        Position hero = state.Table.HeroPosition;
        PotSnapshot snapshot = analysis.Hero;

        int totalRaises = 0;
        int heroRaises = 0;
        int limpers = 0;
        int callersAfterOpen = 0;
        Position? lastAggressor = null;

        foreach (PlayerAction action in state.Actions)
        {
            if (action.Street != Street.Preflop)
            {
                continue;
            }

            bool isAggression = action.Kind is PlayerActionKind.Bet or PlayerActionKind.Raise;

            if (isAggression)
            {
                totalRaises++;
                callersAfterOpen = 0;

                if (action.Position == hero)
                {
                    heroRaises++;
                }
                else
                {
                    lastAggressor = action.Position;
                }

                continue;
            }

            if (action.Kind != PlayerActionKind.Call || action.Position == hero)
            {
                continue;
            }

            if (totalRaises == 0)
            {
                limpers++;
            }
            else
            {
                callersAfterOpen++;
            }
        }

        PreflopContext context = DetermineContext(
            snapshot.DepthInBigBlinds,
            jamThresholdInBigBlinds,
            totalRaises,
            heroRaises,
            limpers,
            callersAfterOpen);

        return new PreflopSituation
        {
            Context = context,
            Relation = lastAggressor is null ? null : RelationOf(state.Table.PlayerCount, hero, lastAggressor.Value),
            Aggressor = lastAggressor,
            PlayersLeftToAct = PositionLayout.PlayersLeftToActPreflop(state.Table.PlayerCount, hero),
            DepthInBigBlinds = snapshot.DepthInBigBlinds,
            AmountToCallInBigBlinds = snapshot.AmountToCallInBigBlinds,
            PotInBigBlinds = snapshot.PotInBigBlinds,
            Limpers = limpers,
        };
    }

    public static FacingRelation RelationOf(int playerCount, Position hero, Position aggressor)
    {
        if (hero == Position.BigBlind)
        {
            return FacingRelation.BigBlind;
        }

        if (hero == Position.SmallBlind)
        {
            return FacingRelation.SmallBlind;
        }

        return PositionLayout.ActsAfterPostflop(playerCount, hero, aggressor)
            ? FacingRelation.InPosition
            : FacingRelation.OutOfPosition;
    }

    private static PreflopContext DetermineContext(
        double depthInBigBlinds,
        double jamThresholdInBigBlinds,
        int totalRaises,
        int heroRaises,
        int limpers,
        int callersAfterOpen)
    {
        if (depthInBigBlinds <= jamThresholdInBigBlinds)
        {
            return totalRaises == 0 ? PreflopContext.Jam : PreflopContext.CallJam;
        }

        if (totalRaises == 0)
        {
            return limpers > 0 ? PreflopContext.VersusLimp : PreflopContext.RaiseFirstIn;
        }

        if (totalRaises == 1)
        {
            return callersAfterOpen > 0 ? PreflopContext.Squeeze : PreflopContext.VersusOpen;
        }

        return totalRaises == 2 && heroRaises == 0
            ? PreflopContext.VersusThreeBet
            : totalRaises >= 3
                ? PreflopContext.VersusFourBet
                : PreflopContext.VersusThreeBet;
    }
}
