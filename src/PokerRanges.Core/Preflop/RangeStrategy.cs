using PokerRanges.Core.Cards;
using PokerRanges.Core.Ranges;

namespace PokerRanges.Core.Preflop;

/// <summary>
/// What a chart says to do, combo by combo: a frequency distribution over the actions. Folding is
/// never written in the data, it is whatever remains once the other actions are subtracted — so
/// no hand can be forgotten.
/// </summary>
public sealed class RangeStrategy
{
    private readonly IReadOnlyList<StrategyBranch> _branches;

    private RangeStrategy(IReadOnlyList<StrategyBranch> branches)
    {
        _branches = branches;
    }

    public static RangeStrategy FromChart(PreflopChart chart)
    {
        ArgumentNullException.ThrowIfNull(chart);

        List<StrategyBranch> branches = [];

        foreach (ChartAction action in chart.Actions)
        {
            if (action.Kind == ChartActionKind.Fold)
            {
                throw new PreflopChartException(
                    $"Le chart « {chart.Describe()} » déclare une action Fold : le fold est déduit de ce qui reste, il ne s'écrit pas.");
            }

            try
            {
                branches.Add(new StrategyBranch(
                    action.Kind,
                    action.SizeInBigBlinds,
                    RangeNotationParser.Parse(action.Range)));
            }
            catch (RangeNotationException exception)
            {
                throw new PreflopChartException(
                    $"Le chart « {chart.Describe()} » contient une range illisible pour l'action {action.Kind}.",
                    exception);
            }
        }

        return new RangeStrategy(branches);
    }

    public HandRange RangeOf(ChartActionKind kind)
    {
        foreach (StrategyBranch branch in _branches)
        {
            if (branch.Kind == kind)
            {
                return branch.Range;
            }
        }

        return HandRange.Empty;
    }

    public IReadOnlyList<StrategyOption> OptionsFor(HoleCards combo)
    {
        List<StrategyOption> options = [];
        double committed = 0;

        foreach (StrategyBranch branch in _branches)
        {
            double frequency = branch.Range.GetWeight(combo);
            if (frequency > 0)
            {
                options.Add(new StrategyOption(branch.Kind, branch.SizeInBigBlinds, frequency));
                committed += frequency;
            }
        }

        return Complete(options, committed);
    }

    /// <summary>Frequencies averaged over the cell's combos: what the 13x13 grid paints.</summary>
    public IReadOnlyList<StrategyOption> OptionsFor(HandClass handClass)
    {
        List<StrategyOption> options = [];
        double committed = 0;

        foreach (StrategyBranch branch in _branches)
        {
            double frequency = branch.Range.FrequencyOf(handClass);
            if (frequency > 0)
            {
                options.Add(new StrategyOption(branch.Kind, branch.SizeInBigBlinds, frequency));
                committed += frequency;
            }
        }

        return Complete(options, committed);
    }

    public StrategyOption MostFrequentFor(HoleCards combo)
    {
        IReadOnlyList<StrategyOption> options = OptionsFor(combo);
        StrategyOption best = options[0];

        foreach (StrategyOption option in options)
        {
            if (option.Frequency > best.Frequency)
            {
                best = option;
            }
        }

        return best;
    }

    private static IReadOnlyList<StrategyOption> Complete(List<StrategyOption> options, double committed)
    {
        double foldFrequency = Math.Clamp(1 - committed, 0, 1);

        if (foldFrequency > 0)
        {
            options.Add(new StrategyOption(ChartActionKind.Fold, 0, foldFrequency));
        }

        if (options.Count == 0)
        {
            options.Add(new StrategyOption(ChartActionKind.Fold, 0, 1));
        }

        return options;
    }
}
