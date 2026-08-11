namespace PokerRanges.Core.Preflop;

public sealed record PreflopAdvisorOptions
{
    /// <summary>
    /// En dessous de cette profondeur on bascule sur les charts de tapis : relancer petit n'a plus
    /// de sens quand la relance engage déjà l'essentiel du tapis.
    /// </summary>
    public double JamThresholdInBigBlinds { get; init; } = 15;

    public static PreflopAdvisorOptions Default { get; } = new();
}
