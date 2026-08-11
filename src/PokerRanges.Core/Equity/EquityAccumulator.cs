using PokerRanges.Core.Evaluation;

namespace PokerRanges.Core.Equity;

internal sealed class EquityAccumulator
{
    private readonly double[] _shareSum;
    private readonly double[] _shareSquareSum;
    private readonly double[] _winSum;
    private readonly double[] _tieSum;

    public EquityAccumulator(int playerCount)
    {
        _shareSum = new double[playerCount];
        _shareSquareSum = new double[playerCount];
        _winSum = new double[playerCount];
        _tieSum = new double[playerCount];
        PlayerCount = playerCount;
    }

    public int PlayerCount { get; }

    public long SampleCount { get; private set; }

    public double WeightSum { get; private set; }

    public void AddShowdown(ReadOnlySpan<HandValue> values, double weight)
    {
        HandValue best = values[0];
        int winnerCount = 1;

        for (int player = 1; player < values.Length; player++)
        {
            if (values[player] > best)
            {
                best = values[player];
                winnerCount = 1;
            }
            else if (values[player] == best)
            {
                winnerCount++;
            }
        }

        double share = 1.0 / winnerCount;

        for (int player = 0; player < values.Length; player++)
        {
            if (values[player] != best)
            {
                continue;
            }

            _shareSum[player] += weight * share;
            _shareSquareSum[player] += weight * share * share;

            if (winnerCount == 1)
            {
                _winSum[player] += weight;
            }
            else
            {
                _tieSum[player] += weight;
            }
        }

        WeightSum += weight;
        SampleCount++;
    }

    public void Merge(EquityAccumulator other)
    {
        for (int player = 0; player < PlayerCount; player++)
        {
            _shareSum[player] += other._shareSum[player];
            _shareSquareSum[player] += other._shareSquareSum[player];
            _winSum[player] += other._winSum[player];
            _tieSum[player] += other._tieSum[player];
        }

        WeightSum += other.WeightSum;
        SampleCount += other.SampleCount;
    }

    public double EquityOf(int player)
    {
        return WeightSum <= 0 ? 0 : _shareSum[player] / WeightSum;
    }

    public double StandardErrorOf(int player)
    {
        if (WeightSum <= 0 || SampleCount < 2)
        {
            return double.PositiveInfinity;
        }

        double mean = EquityOf(player);
        double variance = Math.Max(0, (_shareSquareSum[player] / WeightSum) - (mean * mean));

        return Math.Sqrt(variance / SampleCount);
    }

    public IReadOnlyList<PlayerEquity> ToPlayerEquities()
    {
        PlayerEquity[] equities = new PlayerEquity[PlayerCount];
        for (int player = 0; player < PlayerCount; player++)
        {
            equities[player] = new PlayerEquity(
                EquityOf(player),
                WeightSum <= 0 ? 0 : _winSum[player] / WeightSum,
                WeightSum <= 0 ? 0 : _tieSum[player] / WeightSum);
        }

        return equities;
    }
}
