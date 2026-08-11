using Microsoft.Extensions.Logging;

namespace PokerRanges.App.Infrastructure.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly FileLogWriter _writer;
    private readonly LogLevel _minimumLevel;

    public FileLoggerProvider(string filePath, LogLevel minimumLevel)
    {
        _writer = new FileLogWriter(filePath);
        _minimumLevel = minimumLevel;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _writer, _minimumLevel);
    }

    public void Dispose()
    {
        _writer.Dispose();
    }
}
