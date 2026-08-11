namespace PokerRanges.Data;

/// <summary>
/// À quel point un chart s'écarte de la situation demandée. Les critères sont hiérarchisés :
/// la relation à l'agresseur prime sur le nombre de joueurs derrière, qui prime sur la profondeur.
/// </summary>
internal sealed record ChartMatchScore(int RelationPenalty, int PlayersDistance, double DepthDistance)
    : IComparable<ChartMatchScore>
{
    public int CompareTo(ChartMatchScore? other)
    {
        if (other is null)
        {
            return -1;
        }

        int byRelation = RelationPenalty.CompareTo(other.RelationPenalty);
        if (byRelation != 0)
        {
            return byRelation;
        }

        int byPlayers = PlayersDistance.CompareTo(other.PlayersDistance);
        return byPlayers != 0 ? byPlayers : DepthDistance.CompareTo(other.DepthDistance);
    }
}
