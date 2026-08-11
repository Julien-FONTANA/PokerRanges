using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Table;

/// <summary>
/// The table setup: blind structure, antes, and starting stack seat by seat. Uneven stacks are the
/// norm in tournaments, so they are modelled from the outset. All amounts are in chips.
/// </summary>
public sealed record TableConfiguration
{
    public required int PlayerCount { get; init; }

    public required double BigBlind { get; init; }

    public double? SmallBlindOverride { get; init; }

    public AnteStyle AnteStyle { get; init; } = AnteStyle.None;

    public double AnteAmount { get; init; }

    public required IReadOnlyDictionary<Position, double> StartingStacks { get; init; }

    public required Position HeroPosition { get; init; }

    public double SmallBlind => SmallBlindOverride ?? BigBlind / 2;

    public static TableConfiguration Uniform(
        int playerCount,
        double bigBlind,
        double startingStack,
        Position heroPosition)
    {
        Dictionary<Position, double> stacks = [];
        foreach (Position seat in PositionLayout.Seats(playerCount))
        {
            stacks[seat] = startingStack;
        }

        return new TableConfiguration
        {
            PlayerCount = playerCount,
            BigBlind = bigBlind,
            StartingStacks = stacks,
            HeroPosition = heroPosition,
        };
    }

    public double StackOf(Position position)
    {
        if (!StartingStacks.TryGetValue(position, out double stack))
        {
            throw new TableException(TableText.NoStackFor(PositionLayout.Describe(position)));
        }

        return stack;
    }

    public double StackInBigBlinds(Position position)
    {
        return StackOf(position) / BigBlind;
    }

    public void Validate()
    {
        if (BigBlind <= 0)
        {
            throw new TableException(TableText.BigBlindMustBePositive(BigBlind));
        }

        if (SmallBlind < 0 || SmallBlind > BigBlind)
        {
            throw new TableException(TableText.SmallBlindOutOfRange(SmallBlind));
        }

        if (AnteAmount < 0)
        {
            throw new TableException(TableText.AnteMustNotBeNegative(AnteAmount));
        }

        foreach (Position seat in PositionLayout.Seats(PlayerCount))
        {
            if (StackOf(seat) < 0)
            {
                throw new TableException(TableText.StackMustNotBeNegative(PositionLayout.Describe(seat)));
            }
        }

        if (!PositionLayout.IsSeated(PlayerCount, HeroPosition))
        {
            throw new TableException(
                $"Le héros est placé en {PositionLayout.Describe(HeroPosition)}, qui n'existe pas à une table de {PlayerCount} joueurs.");
        }
    }
}
