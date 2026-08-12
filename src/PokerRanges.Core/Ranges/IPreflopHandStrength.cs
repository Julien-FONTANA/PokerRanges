namespace PokerRanges.Core.Ranges;

/// <summary>
/// Orders the 169 starting hands by strength, so that "the top 20% of hands" means something
/// measured rather than a matter of taste.
/// </summary>
public interface IPreflopHandStrength
{
    /// <summary>Strongest first.</summary>
    IReadOnlyList<RankedHandClass> Ordered { get; }

    /// <summary>
    /// The strongest <paramref name="percent"/> of all 1326 combos. The hand that straddles the
    /// cut-off is included at a partial weight rather than rounded in or out.
    /// </summary>
    HandRange TopPercent(double percent);
}
