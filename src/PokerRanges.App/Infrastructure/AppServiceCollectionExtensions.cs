using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PokerRanges.App.Infrastructure.Logging;
using PokerRanges.App.ViewModels;
using PokerRanges.Core;
using PokerRanges.Data;
using PokerRanges.Data.Storage;

namespace PokerRanges.App.Infrastructure;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddPokerRangesApp(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddDebug();
            builder.AddPokerRangesFile(AppPaths.LogFilePath, LogLevel.Information);
        });

        services.AddPokerRangesCore();
        services.AddPokerRangesData(new PreflopChartRepositoryOptions
        {
            UserChartsDirectory = AppPaths.ChartsDirectory,
        });
        services.AddPokerRangesSession(new SessionStoreOptions
        {
            PreferencesFilePath = AppPaths.SettingsFilePath,
            HandFilePath = AppPaths.HandFilePath,
            JournalFilePath = AppPaths.JournalFilePath,
        });

        services.AddSingleton<AdviceCoordinator>();
        services.AddSingleton<HeadToHeadCoordinator>();
        services.AddSingleton<ChartsViewModel>();
        services.AddSingleton<JournalViewModel>();
        services.AddSingleton<HeadToHeadViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        return services;
    }
}
