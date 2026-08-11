using System.Globalization;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Table;

namespace PokerRanges.Core.Session;

/// <summary>
/// Une main jouée et le conseil qu'elle a reçu. L'état complet est conservé, pas seulement un
/// résumé : c'est ce qui permet de recharger la main pour la rejouer autrement, et c'est là tout
/// l'intérêt d'un journal par rapport à un fichier de traces.
/// </summary>
public sealed record JournalEntry
{
    public required DateTimeOffset PlayedAt { get; init; }

    public required HandState Hand { get; init; }

    public required string Advice { get; init; }

    public required IReadOnlyList<string> Rationale { get; init; }

    public string DescribeHand()
    {
        string hero = Hand.HeroCards is HoleCards cards
            ? $"{cards.First.Describe()} {cards.Second.Describe()}"
            : SessionText.UnknownHand;

        string board = Hand.Board.Count == 0
            ? SessionText.BeforeTheFlop
            : string.Join(" ", Hand.Board.Select(card => card.Describe()));

        return SessionText.JournalHand(
            PositionLayout.Describe(Hand.Table.HeroPosition),
            hero,
            board,
            Hand.Table.PlayerCount);
    }

    public string DescribeMoment()
    {
        return PlayedAt.LocalDateTime.ToString("dd/MM HH:mm", CultureInfo.CurrentCulture);
    }
}
