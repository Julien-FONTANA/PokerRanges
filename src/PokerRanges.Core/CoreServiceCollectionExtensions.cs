using Microsoft.Extensions.DependencyInjection;
using PokerRanges.Core.Equity;
using PokerRanges.Core.Evaluation;
using PokerRanges.Core.HeadToHead;
using PokerRanges.Core.Postflop;
using PokerRanges.Core.Ranges;
using PokerRanges.Core.Table;

namespace PokerRanges.Core;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddPokerRangesCore(this IServiceCollection services)
    {
        services.AddSingleton<IHandEvaluator, RankCountHandEvaluator>();
        services.AddSingleton<IEquityCalculator, EquityCalculator>();
        services.AddSingleton<IPotEngine, PotEngine>();
        services.AddSingleton<IMadeHandClassifier, MadeHandClassifier>();
        services.AddSingleton(PostflopOptions.Default);
        services.AddSingleton<IRangeStrengthRanker, RangeStrengthRanker>();
        services.AddSingleton<IRangeAssigner, RangeAssigner>();
        services.AddSingleton<IPostflopAdvisor, EvPostflopAdvisor>();
        services.AddSingleton<IPreflopHandStrength, PreflopHandStrength>();
        services.AddSingleton<IHeadToHeadCalculator, HeadToHeadCalculator>();

        return services;
    }
}
