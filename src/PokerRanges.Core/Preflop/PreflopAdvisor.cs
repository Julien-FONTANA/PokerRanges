using System.Globalization;
using Microsoft.Extensions.Logging;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Table;

namespace PokerRanges.Core.Preflop;

public sealed class PreflopAdvisor : IPreflopAdvisor
{
    private readonly IPreflopChartRepository _charts;
    private readonly IPotEngine _potEngine;
    private readonly PreflopAdvisorOptions _options;
    private readonly ILogger<PreflopAdvisor> _logger;

    public PreflopAdvisor(
        IPreflopChartRepository charts,
        IPotEngine potEngine,
        PreflopAdvisorOptions options,
        ILogger<PreflopAdvisor> logger)
    {
        _charts = charts;
        _potEngine = potEngine;
        _options = options;
        _logger = logger;
    }

    public ChartResolution ResolveChart(HandState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        HandAnalysis analysis = _potEngine.Analyse(state);
        PreflopSituation situation = PreflopSituationReader.Read(state, analysis, _options.JamThresholdInBigBlinds);

        return _charts.Resolve(situation.ToKey());
    }

    public PreflopAdvice Advise(HandState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.HeroCards is not HoleCards heroCards)
        {
            throw new PreflopChartException(PreflopText.HeroCardsRequired);
        }

        HandAnalysis analysis = _potEngine.Analyse(state);
        PreflopSituation situation = PreflopSituationReader.Read(state, analysis, _options.JamThresholdInBigBlinds);
        ChartResolution resolution = _charts.Resolve(situation.ToKey());

        IReadOnlyList<StrategyOption> options = resolution.Strategy.OptionsFor(heroCards);
        StrategyOption recommendation = resolution.Strategy.MostFrequentFor(heroCards);

        _logger.LogInformation(
            "Preflop advice for {Hand} in {Position}: {Action} ({Frequency:P0}) — {Chart}",
            heroCards.ToHandClass(),
            PositionLayout.Describe(state.Table.HeroPosition),
            recommendation.Kind,
            recommendation.Frequency,
            resolution.Describe());

        return new PreflopAdvice(
            recommendation,
            options,
            resolution,
            situation,
            BuildRationale(state, analysis, situation, resolution, heroCards, options));
    }

    private static IReadOnlyList<string> BuildRationale(
        HandState state,
        HandAnalysis analysis,
        PreflopSituation situation,
        ChartResolution resolution,
        HoleCards heroCards,
        IReadOnlyList<StrategyOption> options)
    {
        List<string> lines =
        [
            PreflopText.RationaleSeat(
                heroCards.ToHandClass().ToString(),
                PositionLayout.Describe(state.Table.HeroPosition),
                state.Table.PlayerCount,
                situation.PlayersLeftToAct),
            PreflopText.RationaleDepth(situation.DepthInBigBlinds, situation.PotInBigBlinds),
            resolution.Describe(),
        ];

        if (situation.Aggressor is Position aggressor)
        {
            string relation = situation.Relation is FacingRelation value
                ? $", {PreflopContextLabels.Describe(value)}"
                : string.Empty;

            lines.Add(PreflopText.RationaleAggressor(PositionLayout.Describe(aggressor), relation));

            if (analysis.Hero.IsFacingABet)
            {
                lines.Add(PreflopText.RationaleToCall(
                    situation.AmountToCallInBigBlinds,
                    analysis.Hero.RequiredEquityToCall));
            }
        }

        lines.Add(PreflopText.RationaleChartStrategy(string.Join(
            ", ",
            options.Select(option => $"{option.Describe()} {option.Frequency.ToString("P0", CultureInfo.CurrentCulture)}"))));

        return lines;
    }
}
