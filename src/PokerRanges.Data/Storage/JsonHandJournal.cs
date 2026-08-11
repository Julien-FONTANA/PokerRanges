using Microsoft.Extensions.Logging;
using PokerRanges.Core;
using PokerRanges.Core.Session;

namespace PokerRanges.Data.Storage;

/// <summary>
/// The hand journal, in a single JSON file read at startup and rewritten on every append. A whole
/// file rather than an append at the end: at a hundred hands the cost is negligible, and it avoids
/// ever having to repair a journal cut off mid-line.
/// </summary>
public sealed class JsonHandJournal : IHandJournal
{
    private const string Label = "le journal des mains";

    private readonly SessionStoreOptions _options;
    private readonly ILogger<JsonHandJournal> _logger;
    private readonly List<JournalEntry> _entries = [];

    public JsonHandJournal(SessionStoreOptions options, ILogger<JsonHandJournal> logger)
    {
        _options = options;
        _logger = logger;

        Load();
    }

    public IReadOnlyList<JournalEntry> Entries => _entries;

    public void Append(JournalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _entries.Insert(0, entry);

        while (_entries.Count > _options.JournalCapacity)
        {
            _entries.RemoveAt(_entries.Count - 1);
        }

        Save();

        _logger.LogInformation(
            "Main journalisée : {Hand} — {Advice}",
            entry.DescribeHand(),
            entry.Advice);
    }

    public void Clear()
    {
        _entries.Clear();
        JsonFileStore.Delete(_options.JournalFilePath, Label, _logger);

        _logger.LogInformation("Journal des mains vidé.");
    }

    private void Load()
    {
        List<StoredJournalEntry>? stored = JsonFileStore.Read<List<StoredJournalEntry>>(
            _options.JournalFilePath,
            Label,
            _logger);

        if (stored is null)
        {
            return;
        }

        foreach (StoredJournalEntry entry in stored)
        {
            try
            {
                _entries.Add(new JournalEntry
                {
                    PlayedAt = entry.PlayedAt,
                    Hand = StoredHandMapper.ToHandState(entry.Hand),
                    Advice = entry.Advice,
                    Rationale = entry.Rationale,
                });
            }
            catch (PokerRangesException exception)
            {
                // One unreadable hand must not take the whole journal down with it.
                _logger.LogWarning(exception, "Une entrée du journal a été écartée : {Message}", exception.Message);
            }
        }

        _logger.LogInformation("{Count} mains relues depuis le journal.", _entries.Count);
    }

    private void Save()
    {
        JsonFileStore.Write(
            _options.JournalFilePath,
            _entries.Select(entry => new StoredJournalEntry
            {
                PlayedAt = entry.PlayedAt,
                Hand = StoredHandMapper.ToStored(entry.Hand),
                Advice = entry.Advice,
                Rationale = [.. entry.Rationale],
            }).ToList(),
            Label,
            _logger);
    }
}
