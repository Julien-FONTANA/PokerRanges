using System.Collections.Immutable;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Preflop;

namespace PokerRanges.Data;

/// <summary>
/// Choisit le chart le plus proche de la situation demandée et retient chaque écart consenti.
/// La résolution ne mélange jamais deux charts : elle en désigne un et dit lequel, pour qu'un
/// conseil reste toujours remontable jusqu'à la donnée qui l'a produit.
/// </summary>
internal static class ChartResolver
{
    private static readonly ImmutableDictionary<PreflopContext, PreflopContext> ContextFallbacks =
        new Dictionary<PreflopContext, PreflopContext>
        {
            [PreflopContext.Squeeze] = PreflopContext.VersusOpen,
            [PreflopContext.VersusFourBet] = PreflopContext.VersusThreeBet,
            [PreflopContext.VersusThreeBet] = PreflopContext.VersusOpen,
            [PreflopContext.VersusLimp] = PreflopContext.RaiseFirstIn,
            [PreflopContext.CallJam] = PreflopContext.VersusOpen,
            [PreflopContext.Jam] = PreflopContext.RaiseFirstIn,
        }.ToImmutableDictionary();

    public static ChartResolution Resolve(
        IReadOnlyList<PreflopChart> charts,
        ChartKey key,
        Func<PreflopChart, RangeStrategy> strategyFactory)
    {
        List<string> adjustments = [];
        PreflopContext context = key.Context;
        List<PreflopChart> candidates = [.. charts.Where(chart => chart.Context == context)];

        while (candidates.Count == 0 && ContextFallbacks.TryGetValue(context, out PreflopContext fallback))
        {
            adjustments.Add(PreflopText.FallbackContext(
                PreflopContextLabels.Describe(context),
                PreflopContextLabels.Describe(fallback)));
            context = fallback;
            candidates = [.. charts.Where(chart => chart.Context == context)];
        }

        if (candidates.Count == 0)
        {
            throw new PreflopChartException(PreflopText.NoChartCovers(key.Describe()));
        }

        PreflopChart best = SelectBest(candidates, key);

        if (key.Relation is not null && best.Relation is not null && best.Relation != key.Relation)
        {
            adjustments.Add(PreflopText.FallbackRelation(
                PreflopContextLabels.Describe(key.Relation.Value),
                PreflopContextLabels.Describe(best.Relation.Value)));
        }

        if (best.PlayersLeftToAct != key.PlayersLeftToAct)
        {
            adjustments.Add(PreflopText.FallbackPlayersLeft(key.PlayersLeftToAct, best.PlayersLeftToAct));
        }

        if (Math.Abs(best.DepthInBigBlinds - key.DepthInBigBlinds) > 0.05)
        {
            adjustments.Add(PreflopText.FallbackDepth(key.DepthInBigBlinds, best.DepthInBigBlinds));
        }

        return new ChartResolution(best, key, strategyFactory(best), adjustments);
    }

    private static PreflopChart SelectBest(List<PreflopChart> candidates, ChartKey key)
    {
        PreflopChart best = candidates[0];
        ChartMatchScore bestScore = Score(best, key);

        foreach (PreflopChart candidate in candidates)
        {
            ChartMatchScore score = Score(candidate, key);

            if (score.CompareTo(bestScore) < 0)
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    private static ChartMatchScore Score(PreflopChart chart, ChartKey key)
    {
        int relationPenalty = (chart.Relation, key.Relation) switch
        {
            (null, null) => 0,
            (null, not null) => 1,
            (not null, null) => 2,
            _ => chart.Relation == key.Relation ? 0 : 3,
        };

        return new ChartMatchScore(
            relationPenalty,
            Math.Abs(chart.PlayersLeftToAct - key.PlayersLeftToAct),
            Math.Abs(chart.DepthInBigBlinds - key.DepthInBigBlinds));
    }
}
