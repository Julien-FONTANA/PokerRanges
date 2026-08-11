using PokerRanges.Core.Session;

namespace PokerRanges.App.ViewModels;

public sealed record JournalEntryViewModel(JournalEntry Entry)
{
    public string Moment => Entry.DescribeMoment();

    public string Hand => Entry.DescribeHand();

    public string Advice => Entry.Advice;
}
