using Microsoft.Extensions.DependencyInjection;
using PokerRanges.Core.Preflop;
using PokerRanges.Core.Session;
using PokerRanges.Data.Storage;

namespace PokerRanges.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddPokerRangesData(
        this IServiceCollection services,
        PreflopChartRepositoryOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IPreflopChartRepository, JsonPreflopChartRepository>();
        services.AddSingleton(PreflopAdvisorOptions.Default);
        services.AddSingleton<IPreflopAdvisor, PreflopAdvisor>();

        return services;
    }

    public static IServiceCollection AddPokerRangesSession(
        this IServiceCollection services,
        SessionStoreOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<ISessionStore, JsonSessionStore>();
        services.AddSingleton<IHandJournal, JsonHandJournal>();

        return services;
    }
}
