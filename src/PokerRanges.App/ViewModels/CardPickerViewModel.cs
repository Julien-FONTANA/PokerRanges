using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PokerRanges.App.Localization;
using PokerRanges.Core.Cards;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// A grid of 52 cards used to build a fixed-size selection. Clicking one card too many replaces
/// the oldest, which saves having to clear between two hands. The same selection can also be
/// typed — "askd" for A♠ K♦ — with both views kept in step.
/// </summary>
public sealed partial class CardPickerViewModel : ObservableObject
{
    private readonly List<CardOptionViewModel> _picked = [];
    private readonly CardOptionViewModel[] _byCardIndex = new CardOptionViewModel[Card.Count];
    private readonly Func<string> _emptyLabel;

    /// <summary>
    /// True while one of the two inputs is updating the other: without this guard, rewriting the
    /// text on every keystroke would wipe the orphan rank the user has just typed.
    /// </summary>
    private bool _isSyncing;

    [ObservableProperty]
    private IReadOnlyList<Card> _selection = [];

    [ObservableProperty]
    private string _label;

    [ObservableProperty]
    private string _quickEntry = string.Empty;

    [ObservableProperty]
    private string? _entryError;

    public CardPickerViewModel(int capacity, Func<string> emptyLabel)
    {
        Capacity = capacity;
        _emptyLabel = emptyLabel;
        _label = emptyLabel();

        List<CardOptionViewModel> options = [];
        foreach (Suit suit in (Suit[])[Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs])
        {
            foreach (Rank rank in Deck.RanksHighToLow)
            {
                CardOptionViewModel option = new(new Card(rank, suit));
                options.Add(option);
                _byCardIndex[option.Card.Index] = option;
            }
        }

        Cards = options;
    }

    public UiText Text => UiText.Current;

    public int Capacity { get; }

    public IReadOnlyList<CardOptionViewModel> Cards { get; }

    public HoleCards? AsHoleCards => Selection.Count == 2 ? new HoleCards(Selection[0], Selection[1]) : null;

    public bool HasEntryError => EntryError is not null;

    /// <summary>Rewrites the label after a language change.</summary>
    public void RefreshLabel()
    {
        Publish();
    }

    /// <summary>Greys out cards already used elsewhere, and drops those that become so.</summary>
    public void SetUnavailable(IReadOnlyCollection<Card> unavailable)
    {
        bool changed = false;

        foreach (CardOptionViewModel option in Cards)
        {
            bool blocked = unavailable.Contains(option.Card);
            option.IsAvailable = !blocked;

            if (blocked && _picked.Remove(option))
            {
                option.IsSelected = false;
                changed = true;
            }
        }

        if (changed)
        {
            Publish();
        }
    }

    [RelayCommand]
    public void Toggle(CardOptionViewModel? option)
    {
        if (option is null || !option.IsAvailable)
        {
            return;
        }

        if (_picked.Remove(option))
        {
            option.IsSelected = false;
            Publish();
            return;
        }

        if (_picked.Count == Capacity)
        {
            _picked[0].IsSelected = false;
            _picked.RemoveAt(0);
        }

        option.IsSelected = true;
        _picked.Add(option);
        Publish();
    }

    /// <summary>
    /// Forces in a selection from elsewhere: resuming an interrupted hand, reloading from the
    /// journal. Unlike keyboard entry, availability is not checked — the reloaded state is
    /// authoritative, and it is up to the caller to clear both pickers before filling them.
    /// </summary>
    public void Restore(IReadOnlyList<Card> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        EntryError = null;
        Select(cards);
    }

    [RelayCommand]
    public void Clear()
    {
        foreach (CardOptionViewModel option in _picked)
        {
            option.IsSelected = false;
        }

        _picked.Clear();
        Publish();
    }

    partial void OnQuickEntryChanged(string value)
    {
        if (_isSyncing)
        {
            return;
        }

        _isSyncing = true;

        try
        {
            ReadText(value);
        }
        finally
        {
            _isSyncing = false;
        }
    }

    partial void OnEntryErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HasEntryError));
    }

    /// <summary>
    /// Re-reads the keyboard entry. A rank without its suit is not a mistake: it is a keystroke in
    /// progress, and the cards already complete are applied without waiting for the rest.
    /// </summary>
    private void ReadText(string value)
    {
        CardSequence sequence = CardSequence.Read(value, Capacity);

        if (sequence.HasError)
        {
            EntryError = sequence.Error;
            return;
        }

        foreach (Card card in sequence.Cards)
        {
            if (!OptionOf(card).IsAvailable)
            {
                EntryError = UiMatrixText.CardAlreadyUsed(card);
                return;
            }
        }

        EntryError = null;
        Select(sequence.Cards);
    }

    private void Select(IReadOnlyList<Card> cards)
    {
        foreach (CardOptionViewModel option in _picked)
        {
            option.IsSelected = false;
        }

        _picked.Clear();

        foreach (Card card in cards)
        {
            CardOptionViewModel option = OptionOf(card);
            option.IsSelected = true;
            _picked.Add(option);
        }

        Publish();
    }

    private CardOptionViewModel OptionOf(Card card)
    {
        return _byCardIndex[card.Index];
    }

    /// <summary>
    /// The label is set before the selection: publishing the selection wakes the subscribers, and
    /// those reading the label on the way must find the card that has just been added.
    /// </summary>
    private void Publish()
    {
        Label = _picked.Count == 0
            ? _emptyLabel()
            : string.Join("  ", _picked.Select(option => option.Label));
        Selection = [.. _picked.Select(option => option.Card)];

        if (_isSyncing)
        {
            return;
        }

        _isSyncing = true;
        EntryError = null;
        QuickEntry = CardSequence.Write(Selection);
        _isSyncing = false;
    }
}
