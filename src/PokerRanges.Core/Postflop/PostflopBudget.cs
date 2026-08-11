using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Postflop;

/// <summary>
/// The computing budget allowed for one piece of advice. It is deliberately distinct from
/// <see cref="PostflopOptions"/>: that one describes what is assumed about the game, this one what
/// we are willing to spend measuring it. Compact mode aims to answer in under a second and pays for
/// that speed in precision — hence the requirement that the advice state the precision it reached.
/// </summary>
public sealed record PostflopBudget
{
    private readonly Func<string> _name;

    private PostflopBudget(Func<string> name, int rankingSamplesPerCombo, int equitySamples)
    {
        _name = name;
        RankingSamplesPerCombo = rankingSamplesPerCombo;
        EquitySamples = equitySamples;
    }

    public string Name => _name();

    /// <summary>Samples per combo when ranking a range by strength on the board.</summary>
    public int RankingSamplesPerCombo { get; init; }

    /// <summary>Monte-Carlo samples per equity calculation.</summary>
    public int EquitySamples { get; init; }

    /// <summary>Analysis budget: take the time precision needs.</summary>
    public static PostflopBudget Full { get; } = new(() => PostflopText.BudgetFull, 250, 30_000);

    /// <summary>
    /// Compact mode budget. Dividing the sample count by four only doubles the standard error —
    /// the square root works in our favour — which makes the trade worth it when you are at the
    /// table waiting for the answer.
    /// </summary>
    public static PostflopBudget Fast { get; } = new(() => PostflopText.BudgetFast, 60, 5_000);

    public string Describe()
    {
        return PostflopText.BudgetSummary(Name, EquitySamples, RankingSamplesPerCombo);
    }
}
