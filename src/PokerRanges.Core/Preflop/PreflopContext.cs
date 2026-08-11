namespace PokerRanges.Core.Preflop;

public enum PreflopContext
{
    /// <summary>Personne n'a ouvert : le héros ouvre ou passe.</summary>
    RaiseFirstIn,

    /// <summary>Un ou plusieurs joueurs ont suivi la grosse blinde sans relancer.</summary>
    VersusLimp,

    /// <summary>Un joueur a ouvert, personne n'a suivi.</summary>
    VersusOpen,

    /// <summary>Un joueur a ouvert et au moins un autre a suivi.</summary>
    Squeeze,

    /// <summary>Le héros a ouvert et se fait relancer.</summary>
    VersusThreeBet,

    /// <summary>Le héros a relancé une ouverture et se fait re-relancer.</summary>
    VersusFourBet,

    /// <summary>Tapis court : le héros part à tapis ou passe.</summary>
    Jam,

    /// <summary>Tapis court : le héros doit décider de payer un tapis adverse.</summary>
    CallJam,
}
