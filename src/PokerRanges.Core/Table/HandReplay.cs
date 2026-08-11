using System.Collections.Immutable;
using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Table;

/// <summary>
/// Rejoue une main action par action pour reconstituer l'état des enchères : mises engagées,
/// tapis restants, joueurs couchés ou à tapis, et qui doit encore parler.
/// </summary>
internal sealed class HandReplay
{
    private const double Tolerance = 1e-6;

    private readonly Dictionary<Position, double> _committed = [];
    private readonly Dictionary<Position, double> _streetCommitted = [];
    private readonly Dictionary<Position, double> _remaining = [];
    private readonly HashSet<Position> _folded = [];
    private readonly HashSet<Position> _actedSinceLastAggression = [];

    private HandReplay(TableConfiguration table)
    {
        Table = table;

        foreach (Position seat in PositionLayout.Seats(table.PlayerCount))
        {
            _committed[seat] = 0;
            _streetCommitted[seat] = 0;
            _remaining[seat] = table.StackOf(seat);
        }
    }

    public TableConfiguration Table { get; }

    public Street Street { get; private set; } = Street.Preflop;

    public double CurrentBet { get; private set; }

    public Position? LastActor { get; private set; }

    public double Pot => _committed.Values.Sum();

    /// <summary>
    /// Rejoue la main. C'est le board qui fait autorité sur la street : trois cartes étalées
    /// signifient que le tour d'enchères préflop est clos, même si aucune action du flop n'a encore
    /// été saisie. Une action datée d'une street que le board n'a pas atteinte est donc une
    /// incohérence de saisie, et elle est refusée.
    /// </summary>
    public static HandReplay Run(HandState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.Table.Validate();

        Street boardStreet = state.CurrentStreet;
        HandReplay replay = new(state.Table);
        replay.PostAntes();
        replay.PostBlinds();

        foreach (PlayerAction action in state.Actions)
        {
            if (action.Street > boardStreet)
            {
                throw new TableException(
                    $"Une action est datée du {action.Street} alors que le board n'en compte que {state.Board.Count} cartes.");
            }

            replay.Apply(action);
        }

        if (replay.Street < boardStreet)
        {
            replay.OpenStreet(boardStreet);
        }

        return replay;
    }

    public double CommittedBy(Position position) => _committed[position];

    public double StreetCommittedBy(Position position) => _streetCommitted[position];

    public double RemainingOf(Position position) => _remaining[position];

    public bool HasFolded(Position position) => _folded.Contains(position);

    public bool IsAllIn(Position position) => _remaining[position] <= Tolerance;

    public bool IsLive(Position position) => !HasFolded(position);

    public bool CanStillAct(Position position) => IsLive(position) && !IsAllIn(position);

    public ImmutableArray<Position> LivePositions()
    {
        return [.. PositionLayout.Seats(Table.PlayerCount).Where(IsLive)];
    }

    public double AmountToCallFor(Position position)
    {
        double owed = CurrentBet - _streetCommitted[position];
        return Math.Max(0, Math.Min(owed, _remaining[position]));
    }

    public Position? NextToAct()
    {
        if (LivePositions().Length < 2)
        {
            return null;
        }

        ImmutableArray<Position> order = PositionLayout.ActionOrder(Table.PlayerCount, Street);
        int start = LastActor is null ? 0 : order.IndexOf(LastActor.Value) + 1;

        for (int step = 0; step < order.Length; step++)
        {
            Position candidate = order[(start + step) % order.Length];

            if (CanStillAct(candidate) && !_actedSinceLastAggression.Contains(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private void PostAntes()
    {
        if (Table.AnteStyle == AnteStyle.None || Table.AnteAmount <= 0)
        {
            return;
        }

        if (Table.AnteStyle == AnteStyle.BigBlindAnte)
        {
            Commit(Position.BigBlind, Table.AnteAmount, countsTowardCurrentBet: false);
            return;
        }

        foreach (Position seat in PositionLayout.Seats(Table.PlayerCount))
        {
            Commit(seat, Table.AnteAmount, countsTowardCurrentBet: false);
        }
    }

    private void PostBlinds()
    {
        Commit(Position.SmallBlind, Table.SmallBlind, countsTowardCurrentBet: true);
        Commit(Position.BigBlind, Table.BigBlind, countsTowardCurrentBet: true);
    }

    private void Apply(PlayerAction action)
    {
        if (!PositionLayout.IsSeated(Table.PlayerCount, action.Position))
        {
            throw new TableException(
                $"{PositionLayout.Describe(action.Position)} n'est pas assis à une table de {Table.PlayerCount} joueurs.");
        }

        if (action.Street < Street)
        {
            throw new TableException(
                $"L'action de {PositionLayout.Describe(action.Position)} est datée du {action.Street} alors que la main en est déjà au {Street}.");
        }

        if (action.Street != Street)
        {
            OpenStreet(action.Street);
        }

        if (HasFolded(action.Position))
        {
            throw new TableException(TableText.AlreadyFolded(PositionLayout.Describe(action.Position)));
        }

        switch (action.Kind)
        {
            case PlayerActionKind.Fold:
                _folded.Add(action.Position);
                break;

            case PlayerActionKind.Check:
                if (AmountToCallFor(action.Position) > Tolerance)
                {
                    throw new TableException(
                        $"{PositionLayout.Describe(action.Position)} ne peut pas checker : il doit encore {AmountToCallFor(action.Position)}.");
                }

                break;

            case PlayerActionKind.Call:
                Commit(action.Position, AmountToCallFor(action.Position), countsTowardCurrentBet: true);
                break;

            case PlayerActionKind.Bet:
            case PlayerActionKind.Raise:
                ApplyAggression(action);
                break;

            default:
                throw new TableException(TableText.UnknownAction(action.Kind));
        }

        _actedSinceLastAggression.Add(action.Position);
        LastActor = action.Position;
    }

    private void ApplyAggression(PlayerAction action)
    {
        double increment = action.AmountTo - _streetCommitted[action.Position];

        if (increment <= Tolerance)
        {
            throw new TableException(
                $"{PositionLayout.Describe(action.Position)} relance à {action.AmountTo} alors qu'il a déjà engagé {_streetCommitted[action.Position]} sur cette street.");
        }

        if (action.AmountTo < CurrentBet - Tolerance && increment < _remaining[action.Position] - Tolerance)
        {
            throw new TableException(
                $"{PositionLayout.Describe(action.Position)} relance à {action.AmountTo}, en dessous de la mise courante de {CurrentBet}, sans être à tapis.");
        }

        Commit(action.Position, increment, countsTowardCurrentBet: true);
        _actedSinceLastAggression.Clear();
    }

    private void OpenStreet(Street street)
    {
        Street = street;
        CurrentBet = 0;
        LastActor = null;
        _actedSinceLastAggression.Clear();

        foreach (Position seat in PositionLayout.Seats(Table.PlayerCount))
        {
            _streetCommitted[seat] = 0;
        }
    }

    private void Commit(Position position, double amount, bool countsTowardCurrentBet)
    {
        double paid = Math.Max(0, Math.Min(amount, _remaining[position]));

        _remaining[position] -= paid;
        _committed[position] += paid;

        if (!countsTowardCurrentBet)
        {
            return;
        }

        _streetCommitted[position] += paid;
        CurrentBet = Math.Max(CurrentBet, _streetCommitted[position]);
    }
}
