namespace PokerRanges.Data;

/// <summary>
/// How far a chart departs from the requested situation. The criteria are ranked: the relation to
/// the aggressor outranks the number of players behind, which outranks the depth.
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
