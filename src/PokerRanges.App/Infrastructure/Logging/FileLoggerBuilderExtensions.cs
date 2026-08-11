using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace PokerRanges.App.Infrastructure.Logging;

public static class FileLoggerBuilderExtensions
{
    public static ILoggingBuilder AddPokerRangesFile(
        this ILoggingBuilder builder,
        string filePath,
        LogLevel minimumLevel)
    {
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider>(new FileLoggerProvider(filePath, minimumLevel)));

        return builder;
    }
}
