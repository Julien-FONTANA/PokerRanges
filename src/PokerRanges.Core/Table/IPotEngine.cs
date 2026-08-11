namespace PokerRanges.Core.Table;

public interface IPotEngine
{
    HandAnalysis Analyse(HandState state);
}
