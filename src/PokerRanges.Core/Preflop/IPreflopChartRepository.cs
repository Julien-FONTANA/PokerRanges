namespace PokerRanges.Core.Preflop;

public interface IPreflopChartRepository
{
    IReadOnlyList<PreflopChart> Charts { get; }

    /// <summary>Dossier des charts éditables, nul quand seuls les charts livrés sont utilisés.</summary>
    string? EditableDirectory { get; }

    ChartResolution Resolve(ChartKey key);

    void Reload();

    /// <summary>
    /// Réécrit les charts livrés par-dessus le dossier éditable et recharge. Le filet de sécurité
    /// de l'édition à la main : on peut casser une range sans avoir à réinstaller l'application.
    /// Renvoie le nombre de fichiers réécrits.
    /// </summary>
    int RestoreDefaults();
}
