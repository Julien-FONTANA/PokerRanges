using PokerRanges.Core.Cards;
using PokerRanges.Core.Evaluation;

namespace PokerRanges.Core.Equity;

/// <summary>
/// Walks every compatible combo assignment and every remaining board runout. An exact result, with
/// no sampling error — reserved for situations whose volume stays manageable.
/// </summary>
internal sealed class ExhaustiveEquityEvaluator
{
    private const long CancellationCheckInterval = 65_536;

    private readonly IHandEvaluator _evaluator;
    private readonly PlayerCombos[] _players;
    private readonly Card[] _board = new Card[5];
    private readonly int _knownBoardCount;
    private readonly bool[] _used;
    private readonly HoleCards[] _assigned;
    private readonly HandValue[] _values;
    private readonly Card[] _hand = new Card[7];
    private readonly EquityAccumulator _accumulator;
    private readonly CancellationToken _cancellationToken;

    private long _showdowns;

    public ExhaustiveEquityEvaluator(
        IHandEvaluator evaluator,
        PlayerCombos[] players,
        IReadOnlyList<Card> knownBoard,
        bool[] blockedCards,
        CancellationToken cancellationToken)
    {
        _evaluator = evaluator;
        _players = players;
        _knownBoardCount = knownBoard.Count;
        _used = (bool[])blockedCards.Clone();
        _assigned = new HoleCards[players.Length];
        _values = new HandValue[players.Length];
        _accumulator = new EquityAccumulator(players.Length);
        _cancellationToken = cancellationToken;

        for (int index = 0; index < knownBoard.Count; index++)
        {
            _board[index] = knownBoard[index];
        }
    }

    public EquityAccumulator Run()
    {
        AssignPlayer(0, 1.0);
        return _accumulator;
    }

    private void AssignPlayer(int playerIndex, double weight)
    {
        if (playerIndex == _players.Length)
        {
            EnumerateRunout(_knownBoardCount, 0, weight);
            return;
        }

        PlayerCombos player = _players[playerIndex];

        for (int index = 0; index < player.Length; index++)
        {
            HoleCards combo = player.Combos[index];
            int first = combo.First.Index;
            int second = combo.Second.Index;

            if (_used[first] || _used[second])
            {
                continue;
            }

            _used[first] = true;
            _used[second] = true;
            _assigned[playerIndex] = combo;

            AssignPlayer(playerIndex + 1, weight * player.Weights[index]);

            _used[first] = false;
            _used[second] = false;
        }
    }

    private void EnumerateRunout(int filled, int firstCandidate, double weight)
    {
        if (filled == _board.Length)
        {
            Score(weight);
            return;
        }

        for (int index = firstCandidate; index < Card.Count; index++)
        {
            if (_used[index])
            {
                continue;
            }

            _used[index] = true;
            _board[filled] = Card.FromIndex(index);

            EnumerateRunout(filled + 1, index + 1, weight);

            _used[index] = false;
        }
    }

    private void Score(double weight)
    {
        for (int player = 0; player < _players.Length; player++)
        {
            _hand[0] = _assigned[player].First;
            _hand[1] = _assigned[player].Second;
            for (int boardIndex = 0; boardIndex < _board.Length; boardIndex++)
            {
                _hand[2 + boardIndex] = _board[boardIndex];
            }

            _values[player] = _evaluator.Evaluate(_hand);
        }

        _accumulator.AddShowdown(_values, weight);

        _showdowns++;
        if (_showdowns % CancellationCheckInterval == 0)
        {
            _cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
