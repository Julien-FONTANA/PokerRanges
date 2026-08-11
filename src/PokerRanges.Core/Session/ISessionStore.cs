using PokerRanges.Core.Table;

namespace PokerRanges.Core.Session;

/// <summary>
/// Retient d'un lancement à l'autre les réglages et la main en cours. Les lectures ne lèvent
/// jamais : un fichier illisible vaut « rien de sauvegardé », parce qu'un assistant qui refuse de
/// démarrer à cause de son propre fichier de reprise est pire qu'un assistant qui a tout oublié.
/// </summary>
public interface ISessionStore
{
    UserPreferences LoadPreferences();

    void SavePreferences(UserPreferences preferences);

    /// <summary>La main interrompue au dernier arrêt, ou null s'il n'y en a pas.</summary>
    HandState? LoadHand();

    /// <summary>Passer null efface la reprise : la main est finie, il n'y a plus rien à reprendre.</summary>
    void SaveHand(HandState? hand);
}
