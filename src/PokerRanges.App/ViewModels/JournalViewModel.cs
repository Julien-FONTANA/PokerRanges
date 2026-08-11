using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PokerRanges.App.Localization;
using PokerRanges.Core.Session;
using PokerRanges.Core.Table;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// Le carnet des mains jouées. Une entrée n'est pas un texte mais la main entière : la recharger
/// remet l'application exactement dans l'état où le conseil avait été donné, ce qui permet de
/// rejouer la décision avec un autre profil adverse ou une autre taille.
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

    /// <summary>Appelé quand l'utilisateur demande à recharger une main du journal.</summary>
    public event EventHandler<JournalEntry>? ReplayRequested;

    public UiText Text => UiText.Current;

    public ObservableCollection<JournalEntryViewModel> Entries { get; } = [];

    public bool IsEmpty => Entries.Count == 0;

    /// <summary>
    /// Journalise la main si elle a de quoi être relue. Une table qu'on règle sans jouer, ou deux
    /// cartes posées puis reprises, n'ont rien à faire dans un carnet de mains.
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

        _logger.LogInformation("Rechargement de la main du {Moment}.", entry.Moment);
        ReplayRequested?.Invoke(this, entry.Entry);
    }

    [RelayCommand]
    public void Clear()
    {
        _journal.Clear();
        Refresh();
    }

    /// <summary>
    /// Reconstruit la liste : les descriptions d'entrées sont calculées, donc elles ne suivent la
    /// langue que si on les redemande.
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
