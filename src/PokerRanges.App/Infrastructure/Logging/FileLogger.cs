using System.Globalization;
using Microsoft.Extensions.Logging;

namespace PokerRanges.App.Infrastructure.Logging;

public sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly FileLogWriter _writer;
    private readonly LogLevel _minimumLevel;

    public FileLogger(string categoryName, FileLogWriter writer, LogLevel minimumLevel)
    {
        _categoryName = categoryName;
        _writer = writer;
        _minimumLevel = minimumLevel;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _minimumLevel && logLevel != LogLevel.None;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        string timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        string message = formatter(state, exception);
        _writer.WriteLine($"{timestamp} [{logLevel}] {_categoryName} - {message}");

        if (exception is not null)
        {
            _writer.WriteLine(exception.ToString());
        }
    }
}
