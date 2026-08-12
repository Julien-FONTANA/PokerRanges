namespace PokerRanges.Core.HeadToHead;

/// <summary>Which side of the all-in the hero is on.</summary>
public enum HeadToHeadRole
{
    /// <summary>The hero moves all-in; the villain chooses to call or fold.</summary>
    Jamming,

    /// <summary>The villain is already all-in; the hero chooses to call or fold.</summary>
    CallingAJam,
}
