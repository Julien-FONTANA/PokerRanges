namespace PokerRanges.Core.Preflop;

public sealed record ChartKey(
    PreflopContext Context,
    int PlayersLeftToAct,
    FacingRelation? Relation,
    double DepthInBigBlinds)
{
    public string Describe()
    {
        string relation = Relation is null ? string.Empty : $" {PreflopContextLabels.Describe(Relation.Value)},";
        return $"{PreflopContextLabels.Describe(Context)},{relation} {PlayersLeftToAct} joueur(s) derrière, {DepthInBigBlinds:0.#}bb";
    }
}
