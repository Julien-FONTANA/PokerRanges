using System.Globalization;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Table;

namespace PokerRanges.Core.Localization;

/// <summary>
/// Les messages de la table et des cartes. Les abréviations de position — BTN, UTG, SB — ne
/// figurent pas ici : elles sont les mêmes dans les deux langues.
/// </summary>
public static class TableText
{
    public static string StreetPreflop => Language.Pick("Preflop", "Préflop");

    public static string StreetFlop => "Flop";

    public static string StreetTurn => "Turn";

    public static string StreetRiver => "River";

    public static string Describe(Street street)
    {
        return street switch
        {
            Street.Preflop => StreetPreflop,
            Street.Flop => StreetFlop,
            Street.Turn => StreetTurn,
            _ => StreetRiver,
        };
    }

    public static string NotSeated(string position)
    {
        return Language.Pick(
            $"{position} is not seated at this table.",
            $"{position} n'est pas assis à cette table.");
    }

    public static string AlreadyFolded(string position)
    {
        return Language.Pick(
            $"{position} has already folded and cannot act again.",
            $"{position} s'est déjà couché et ne peut plus agir.");
    }

    public static string UnknownAction(PlayerActionKind kind)
    {
        return Language.Pick($"Unknown action: {kind}.", $"Action inconnue : {kind}.");
    }

    public static string BoardCardCount(int received)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"A board holds 0, 3, 4 or 5 cards, got {received}."),
            string.Create(CultureInfo.CurrentCulture, $"Un board compte 0, 3, 4 ou 5 cartes, reçu {received}."));
    }

    public static string SeatNotAtTable(Position position, int playerCount)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{position} is not a seat at a {playerCount}-handed table."),
            string.Create(CultureInfo.CurrentCulture, $"{position} n'est pas une position d'une table de {playerCount} joueurs."));
    }

    public static string PlayerCountOutOfRange(int minimum, int maximum, int received)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"The number of players must be between {minimum} and {maximum}, got {received}."),
            string.Create(CultureInfo.CurrentCulture, $"Le nombre de joueurs doit être compris entre {minimum} et {maximum}, reçu {received}."));
    }

    public static string NoStackFor(string position)
    {
        return Language.Pick(
            $"No stack is set for {position}.",
            $"Aucun tapis n'est renseigné pour {position}.");
    }

    public static string BigBlindMustBePositive(double received)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"The big blind must be strictly positive, got {received}."),
            string.Create(CultureInfo.CurrentCulture, $"La grosse blinde doit être strictement positive, reçu {received}."));
    }

    public static string SmallBlindOutOfRange(double received)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"The small blind must sit between 0 and the big blind, got {received}."),
            string.Create(CultureInfo.CurrentCulture, $"La petite blinde doit être comprise entre 0 et la grosse blinde, reçu {received}."));
    }

    public static string AnteMustNotBeNegative(double received)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"The ante cannot be negative, got {received}."),
            string.Create(CultureInfo.CurrentCulture, $"L'ante ne peut pas être négative, reçu {received}."));
    }

    public static string StackMustNotBeNegative(string position)
    {
        return Language.Pick(
            $"{position}'s stack cannot be negative.",
            $"Le tapis de {position} ne peut pas être négatif.");
    }

    public static string InvalidCard(ReadOnlySpan<char> text)
    {
        string received = text.ToString();

        return Language.Pick(
            $"Invalid card: « {received} ». Expected a rank (2-9, T, J, Q, K, A) followed by a suit (c, d, h, s), for instance « As ».",
            $"Carte invalide : « {received} ». Format attendu : rang (2-9, T, J, Q, K, A) suivi de la couleur (c, d, h, s), par exemple « As ».");
    }

    public static string NotARank(char character)
    {
        return Language.Pick(
            $"« {character} » is not a rank. Expected ranks: 2-9, T, J, Q, K, A.",
            $"« {character} » n'est pas un rang. Rangs attendus : 2-9, T, J, Q, K, A.");
    }

    public static string NotASuit(char character)
    {
        return Language.Pick(
            $"« {character} » is not a suit. Expected suits: c (clubs), d (diamonds), h (hearts), s (spades).",
            $"« {character} » n'est pas une couleur. Couleurs attendues : c (trèfle), d (carreau), h (cœur), s (pique).");
    }

    public static string CardTwice(Card card)
    {
        return Language.Pick(
            $"{card} is entered twice: there is only one deck.",
            $"{card} est saisie deux fois : le paquet est unique.");
    }

    public static string TooManyCards(int capacity, Card extra)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"{capacity} cards at most here, {extra} is one too many."),
            string.Create(CultureInfo.CurrentCulture, $"{capacity} cartes au maximum ici, {extra} est en trop."));
    }

    public static string EquityNeedsTwoPlayers(int received)
    {
        return Language.Pick(
            string.Create(CultureInfo.CurrentCulture, $"An equity computation needs at least two players, got {received}."),
            string.Create(CultureInfo.CurrentCulture, $"Le calcul d'équité exige au moins deux joueurs, reçu {received}."));
    }

    public static string CardTwiceInBoard(Card card)
    {
        return Language.Pick(
            $"Card {card} appears twice in the board or the dead cards.",
            $"La carte {card} apparaît deux fois dans le board ou les cartes mortes.");
    }

    public static string RangesOverlapTooMuch => Language.Pick(
        "The players' ranges overlap too much to be dealt together: almost no combination of hands is compatible with the board.",
        "Les ranges des joueurs se recouvrent trop pour pouvoir être distribuées ensemble : il n'existe presque aucune combinaison de mains compatible avec le board.");
}
