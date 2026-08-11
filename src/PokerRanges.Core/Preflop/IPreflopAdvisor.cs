using PokerRanges.Core.Table;

namespace PokerRanges.Core.Preflop;

public interface IPreflopAdvisor
{
    /// <summary>Le chart qui s'applique à cette situation, indépendamment de la main du héros.</summary>
    ChartResolution ResolveChart(HandState state);

    /// <summary>Le conseil pour la main du héros ; exige que ses cartes soient renseignées.</summary>
    PreflopAdvice Advise(HandState state);
}
