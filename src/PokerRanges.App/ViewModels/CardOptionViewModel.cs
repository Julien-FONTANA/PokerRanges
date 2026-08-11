using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using PokerRanges.Core.Cards;

namespace PokerRanges.App.ViewModels;

public sealed partial class CardOptionViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isAvailable = true;

    public CardOptionViewModel(Card card)
    {
        Card = card;
        Label = $"{CardSymbols.ToCharacter(card.Rank)}{CardSymbols.ToGlyph(card.Suit)}";
        Foreground = card.Suit is Suit.Hearts or Suit.Diamonds
            ? new SolidColorBrush(Color.Parse("#E05A5A"))
            : new SolidColorBrush(Color.Parse("#E8E8E8"));
    }

    public Card Card { get; }

    public string Label { get; }

    public IBrush Foreground { get; }
}
