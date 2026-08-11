using System.Text;
using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Cards;

/// <summary>
/// Lit une suite de cartes tapée d'un trait — « askd » donne A♠ K♦ — pour saisir une main sans
/// quitter le clavier. Contrairement à <see cref="Card.Parse"/>, la lecture est tolérante : elle
/// est faite pour être rappelée à chaque frappe, donc un rang encore orphelin en fin de texte est
/// une saisie en cours et non une erreur. Les séparateurs sont ignorés, ce qui laisse écrire
/// « as kd » ou « As,Kd » indifféremment.
/// </summary>
public sealed record CardSequence
{
    private const string Separators = " ,;.-_/|";

    public required IReadOnlyList<Card> Cards { get; init; }

    /// <summary>Le rang tapé dont la couleur manque encore. Vide quand la saisie est complète.</summary>
    public required string Pending { get; init; }

    /// <summary>Renseigné quand le texte contient autre chose qu'une saisie en cours valide.</summary>
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

    /// <summary>Réécrit une sélection dans la forme compacte que la lecture accepte.</summary>
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
