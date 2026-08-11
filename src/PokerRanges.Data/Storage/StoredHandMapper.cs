using PokerRanges.Core.Cards;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Table;

namespace PokerRanges.Data.Storage;

/// <summary>
/// Traduit entre la main du moteur et sa forme sur disque. La relecture valide en même temps
/// qu'elle traduit : un fichier trafiqué doit échouer ici, franchement, plutôt que de produire une
/// main incohérente que le moteur découvrirait trois écrans plus loin.
/// </summary>
public static class StoredHandMapper
{
    public static StoredHand ToStored(HandState hand)
    {
        ArgumentNullException.ThrowIfNull(hand);

        return new StoredHand
        {
            PlayerCount = hand.Table.PlayerCount,
            BigBlind = hand.Table.BigBlind,
            SmallBlind = hand.Table.SmallBlindOverride,
            AnteStyle = hand.Table.AnteStyle,
            AnteAmount = hand.Table.AnteAmount,
            HeroPosition = hand.Table.HeroPosition,
            StartingStacks = new Dictionary<Position, double>(hand.Table.StartingStacks),
            HeroCards = hand.HeroCards?.ToString(),
            Board = CardSequence.Write(hand.Board),
            Actions =
            [
                .. hand.Actions.Select(action => new StoredAction
                {
                    Street = action.Street,
                    Position = action.Position,
                    Kind = action.Kind,
                    AmountTo = action.AmountTo,
                }),
            ],
        };
    }

    public static HandState ToHandState(StoredHand stored)
    {
        ArgumentNullException.ThrowIfNull(stored);

        TableConfiguration table = new()
        {
            PlayerCount = stored.PlayerCount,
            BigBlind = stored.BigBlind,
            SmallBlindOverride = stored.SmallBlind,
            AnteStyle = stored.AnteStyle,
            AnteAmount = stored.AnteAmount,
            HeroPosition = stored.HeroPosition,
            StartingStacks = ReadStacks(stored),
        };

        table.Validate();

        return new HandState
        {
            Table = table,
            HeroCards = ReadHeroCards(stored.HeroCards),
            Board = ReadBoard(stored.Board),
            Actions =
            [
                .. stored.Actions.Select(action => new PlayerAction(
                    action.Street,
                    action.Position,
                    action.Kind,
                    action.AmountTo)),
            ],
        };
    }

    private static IReadOnlyDictionary<Position, double> ReadStacks(StoredHand stored)
    {
        if (stored.StartingStacks.Count > 0)
        {
            return new Dictionary<Position, double>(stored.StartingStacks);
        }

        // Un fichier écrit avant que les tapis inégaux ne soient enregistrés : on repart uniforme
        // plutôt que de refuser la reprise pour une information qu'on sait reconstruire.
        Dictionary<Position, double> uniform = [];
        foreach (Position seat in PositionLayout.Seats(stored.PlayerCount))
        {
            uniform[seat] = stored.BigBlind * 100;
        }

        return uniform;
    }

    private static HoleCards? ReadHeroCards(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        CardSequence sequence = CardSequence.Read(text, 2);

        if (sequence.HasError || sequence.Cards.Count != 2)
        {
            throw new CardFormatException(SessionText.StoredHandUnreadable(text));
        }

        return new HoleCards(sequence.Cards[0], sequence.Cards[1]);
    }

    private static IReadOnlyList<Card> ReadBoard(string text)
    {
        CardSequence sequence = CardSequence.Read(text, 5);

        if (sequence.HasError)
        {
            throw new CardFormatException(SessionText.StoredBoardUnreadable(text, sequence.Error ?? string.Empty));
        }

        return sequence.Cards;
    }
}
