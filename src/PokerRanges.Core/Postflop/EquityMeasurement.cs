namespace PokerRanges.Core.Postflop;

/// <summary>
/// Une équité mesurée et ce qu'elle vaut. La mesure et son incertitude voyagent ensemble : les
/// séparer, c'est perdre la seconde en chemin et présenter une estimation comme un fait.
/// </summary>
public sealed record EquityMeasurement(double Equity, double StandardError)
{
    public static EquityMeasurement Certain(double equity)
    {
        return new EquityMeasurement(equity, 0);
    }
}
