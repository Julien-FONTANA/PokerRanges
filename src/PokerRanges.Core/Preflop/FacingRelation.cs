namespace PokerRanges.Core.Preflop;

/// <summary>
/// La place du héros par rapport à l'agresseur. Les blindes sont distinguées de la simple
/// position parce que leur mise déjà engagée change complètement les cotes de défense.
/// </summary>
public enum FacingRelation
{
    InPosition,
    OutOfPosition,
    SmallBlind,
    BigBlind,
}
