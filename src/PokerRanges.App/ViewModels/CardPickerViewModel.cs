using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PokerRanges.App.Localization;
using PokerRanges.Core.Cards;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// Une grille de 52 cartes servant à composer une sélection de taille fixe. Cliquer une carte de
/// trop remplace la plus ancienne, ce qui évite d'avoir à effacer entre deux mains. La même
/// sélection s'écrit aussi au clavier — « askd » pour A♠ K♦ — les deux vues restant en phase.
/// </summary>
public sealed partial class CardPickerViewModel : ObservableObject
{
    private readonly List<CardOptionViewModel> _picked = [];
    private readonly CardOptionViewModel[] _byCardIndex = new CardOptionViewModel[Card.Count];
    private readonly Func<string> _emptyLabel;

    /// <summary>
    /// Vrai pendant qu'une des deux saisies met l'autre à jour : sans ce garde-fou, réécrire le
    /// texte à chaque frappe effacerait le rang encore orphelin que l'utilisateur vient de taper.
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

    /// <summary>Réécrit le libellé après un changement de langue.</summary>
    public void RefreshLabel()
    {
        Publish();
    }

    /// <summary>Grise les cartes déjà utilisées ailleurs, et retire celles qui le deviennent.</summary>
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
    /// Impose une sélection venue d'ailleurs : reprise d'une main interrompue, rechargement depuis
    /// le journal. Contrairement à la saisie clavier, la disponibilité n'est pas contrôlée — l'état
    /// rechargé fait foi, et c'est à l'appelant de vider les deux sélecteurs avant de les remplir.
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
    /// Relit la saisie clavier. Un rang sans sa couleur n'est pas une faute : c'est une frappe en
    /// cours, et les cartes déjà complètes sont appliquées sans attendre la suite.
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
    /// Le libellé est posé avant la sélection : publier la sélection réveille les abonnés, et ceux
    /// qui lisent le libellé au passage doivent y trouver la carte qui vient d'être ajoutée.
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
