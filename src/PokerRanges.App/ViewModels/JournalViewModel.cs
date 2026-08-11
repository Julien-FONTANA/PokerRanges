using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PokerRanges.App.Localization;
using PokerRanges.Core.Session;
using PokerRanges.Core.Table;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// The log of hands played. An entry is not a piece of text but the whole hand: reloading it puts
/// the application back exactly in the state the advice was given in, which makes it possible to
/// replay the decision with a different opponent profile or a different size.
/// </summary>
public sealed partial class JournalViewModel : ObservableObject
{
    private readonly IHandJournal _journal;
    private readonly ILogger<JournalViewModel> _logger;

    [ObservableProperty]
    private string _summary = string.Empty;

    public JournalViewModel(IHandJournal journal, ILogger<JournalViewModel> logger)
    {
        _journal = journal;
        _logger = logger;

        Refresh();
    }

    /// <summary>Raised when the user asks to reload a hand from the journal.</summary>
    public event EventHandler<JournalEntry>? ReplayRequested;

    public UiText Text => UiText.Current;

    public ObservableCollection<JournalEntryViewModel> Entries { get; } = [];

    public bool IsEmpty => Entries.Count == 0;

    /// <summary>
    /// Journals the hand if there is anything worth reading back. A table set up but never played,
    /// or two cards laid down then taken back, have no place in a hand log.
    /// </summary>
    public void Record(HandState hand, string advice, IReadOnlyList<string> rationale)
    {
        ArgumentNullException.ThrowIfNull(hand);

        if (hand.Actions.Count == 0 || hand.HeroCards is null)
        {
            return;
        }

        _journal.Append(new JournalEntry
        {
            PlayedAt = DateTimeOffset.Now,
            Hand = hand,
            Advice = advice,
            Rationale = rationale,
        });

        Refresh();
    }

    [RelayCommand]
    public void Replay(JournalEntryViewModel? entry)
    {
        if (entry is null)
        {
            return;
        }

        _logger.LogInformation("Reloading the hand from {Moment}.", entry.Moment);
        ReplayRequested?.Invoke(this, entry.Entry);
    }

    [RelayCommand]
    public void Clear()
    {
        _journal.Clear();
        Refresh();
    }

    /// <summary>
    /// Rebuilds the list: entry descriptions are computed, so they only follow the language if
    /// they are asked for again.
    /// </summary>
    public void Refresh()
    {
        Entries.Clear();

        foreach (JournalEntry entry in _journal.Entries)
        {
            Entries.Add(new JournalEntryViewModel(entry));
        }

        Summary = Entries.Count == 0
            ? UiText.Current.EmptyJournal
            : UiMatrixText.JournalCount(Entries.Count);

        OnPropertyChanged(nameof(IsEmpty));
    }
}
