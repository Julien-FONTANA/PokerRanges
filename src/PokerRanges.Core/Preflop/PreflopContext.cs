namespace PokerRanges.Core.Preflop;

public enum PreflopContext
{
    /// <summary>Nobody has opened: the hero opens or folds.</summary>
    RaiseFirstIn,

    /// <summary>One or more players called the big blind without raising.</summary>
    VersusLimp,

    /// <summary>One player opened, nobody called.</summary>
    VersusOpen,

    /// <summary>One player opened and at least one other called.</summary>
    Squeeze,

    /// <summary>The hero opened and is being raised.</summary>
    VersusThreeBet,

    /// <summary>The hero re-raised an open and is being raised again.</summary>
    VersusFourBet,

    /// <summary>Short stack: the hero shoves or folds.</summary>
    Jam,

    /// <summary>Short stack: the hero must decide whether to call an opponent's shove.</summary>
    CallJam,
}
