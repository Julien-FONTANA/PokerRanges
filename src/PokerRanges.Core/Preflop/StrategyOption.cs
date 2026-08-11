using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Preflop;

public sealed record StrategyOption(ChartActionKind Kind, double SizeInBigBlinds, double Frequency)
{
    public string Describe()
    {
        return Kind switch
        {
            ChartActionKind.Fold => PreflopText.OptionFold,
            ChartActionKind.Call => PreflopText.OptionCall,
            ChartActionKind.Jam => PreflopText.OptionJam,
            _ => SizeInBigBlinds > 0 ? PreflopText.OptionRaiseTo(SizeInBigBlinds) : PreflopText.OptionRaise,
        };
    }
}
