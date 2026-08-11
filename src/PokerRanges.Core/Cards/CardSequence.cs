using System.Text;
using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Cards;

/// <summary>
/// Reads a run of cards typed in one go — "askd" gives A♠ K♦ — so a hand can be entered without
/// leaving the keyboard. Unlike <see cref="Card.Parse"/>, reading is forgiving: it is meant to be
/// called again on every keystroke, so a rank still missing its suit at the end of the text is a
/// half-finished entry, not an error. Separators are ignored, which allows "as kd" or "As,Kd"
/// interchangeably.
/// </summary>
public sealed record CardSequence
{
    private const string Separators = " ,;.-_/|";

    public required IReadOnlyList<Card> Cards { get; init; }

    /// <summary>The typed rank whose suit is still missing. Empty when the entry is complete.</summary>
    public required string Pending { get; init; }

    /// <summary>Set when the text holds something other than a valid entry in progress.</summary>
    public string? Error { get; init; }

    public bool HasError => Error is not null;

    public static CardSequence Read(string? text, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        List<Card> cards = [];
        bool[] taken = new bool[Card.Count];
        Rank? pendingRank = null;

        foreach (char character in text ?? string.Empty)
        {
            if (Separators.Contains(character, StringComparison.Ordinal))
            {
                continue;
            }

            if (pendingRank is not Rank rank)
            {
                if (!CardSymbols.TryParseRank(character, out Rank parsed))
                {
                    return Failure(cards, TableText.NotARank(character));
                }

                pendingRank = parsed;
                continue;
            }

            if (!CardSymbols.TryParseSuit(character, out Suit suit))
            {
                return Failure(cards, TableText.NotASuit(character));
            }

            Card card = new(rank, suit);
            pendingRank = null;

            if (taken[card.Index])
            {
                return Failure(cards, TableText.CardTwice(card));
            }

            if (cards.Count == capacity)
            {
                return Failure(cards, TableText.TooManyCards(capacity, card));
            }

            taken[card.Index] = true;
            cards.Add(card);
        }

        return new CardSequence
        {
            Cards = cards,
            Pending = pendingRank is Rank orphan ? CardSymbols.ToCharacter(orphan).ToString() : string.Empty,
        };
    }

    /// <summary>Writes a selection back in the compact form that reading accepts.</summary>
    public static string Write(IReadOnlyCollection<Card> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        StringBuilder builder = new(cards.Count * 2);
        foreach (Card card in cards)
        {
            builder.Append(card);
        }

        return builder.ToString();
    }

    private static CardSequence Failure(IReadOnlyList<Card> cards, string error)
    {
        return new CardSequence
        {
            Cards = cards,
            Pending = string.Empty,
            Error = error,
        };
    }
}
