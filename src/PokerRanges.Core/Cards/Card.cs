using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Cards;

/// <summary>
/// A card from the deck. <see cref="Index"/> (0 to 51) is the key used by every working array in
/// the engine: two of clubs = 0, ace of spades = 51.
/// </summary>
public readonly record struct Card
{
    public const int Count = 52;

    public Card(Rank rank, Suit suit)
    {
        Rank = rank;
        Suit = suit;
    }

    public Rank Rank { get; }

    public Suit Suit { get; }

    public int Index => (((int)Rank - 2) * 4) + (int)Suit;

    public static Card FromIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

        return new Card((Rank)((index / 4) + 2), (Suit)(index % 4));
    }

    public static Card Parse(ReadOnlySpan<char> text)
    {
        if (!TryParse(text, out Card card))
        {
            throw new CardFormatException(TableText.InvalidCard(text));
        }

        return card;
    }

    public static bool TryParse(ReadOnlySpan<char> text, out Card card)
    {
        card = default;
        ReadOnlySpan<char> trimmed = text.Trim();

        if (trimmed.Length != 2
            || !CardSymbols.TryParseRank(trimmed[0], out Rank rank)
            || !CardSymbols.TryParseSuit(trimmed[1], out Suit suit))
        {
            return false;
        }

        card = new Card(rank, suit);
        return true;
    }

    public override string ToString()
    {
        return string.Create(2, this, static (span, card) =>
        {
            span[0] = CardSymbols.ToCharacter(card.Rank);
            span[1] = CardSymbols.ToCharacter(card.Suit);
        });
    }

    /// <summary>
    /// The card with its suit glyph — "K♥". Distinct from <see cref="ToString"/>, which produces
    /// the form <see cref="Parse"/> can read back: what you read and what you type are not the same
    /// thing, and conflating them would break reloading a saved hand.
    /// </summary>
    public string Describe()
    {
        return string.Create(2, this, static (span, card) =>
        {
            span[0] = CardSymbols.ToCharacter(card.Rank);
            span[1] = CardSymbols.ToGlyph(card.Suit);
        });
    }
}
