using System.Text;

namespace PokerRanges.App.Infrastructure.Logging;

public sealed class FileLogWriter : IDisposable
{
    private const long MaximumFileSizeInBytes = 5 * 1024 * 1024;

    private readonly Lock _gate = new();
    private readonly string _filePath;

    private StreamWriter? _writer;

    public FileLogWriter(string filePath)
    {
        _filePath = filePath;
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
        RollIfTooLarge();
        _writer = new StreamWriter(filePath, append: true, Encoding.UTF8) { AutoFlush = true };
    }

    public void WriteLine(string line)
    {
        lock (_gate)
        {
            _writer?.WriteLine(line);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    private void RollIfTooLarge()
    {
        FileInfo current = new(_filePath);
        if (!current.Exists || current.Length < MaximumFileSizeInBytes)
        {
            return;
        }

        string previousPath = _filePath + ".1";
        File.Delete(previousPath);
        File.Move(_filePath, previousPath);
    }
}
