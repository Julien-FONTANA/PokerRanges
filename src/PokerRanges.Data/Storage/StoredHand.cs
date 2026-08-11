using PokerRanges.Core.Table;

namespace PokerRanges.Data.Storage;

/// <summary>
/// La forme sur disque d'une main. Distincte de <see cref="HandState"/> à dessein : le moteur doit
/// pouvoir changer de représentation interne sans rendre illisibles les mains déjà enregistrées.
/// Les cartes y sont du texte — « Ks » — pour qu'un fichier de reprise reste lisible à l'œil.
/// </summary>
public sealed class StoredHand
{
    public int PlayerCount { get; set; } = 8;

    public double BigBlind { get; set; } = 8;

    public double? SmallBlind { get; set; }

    public AnteStyle AnteStyle { get; set; } = AnteStyle.None;

    public double AnteAmount { get; set; }

    public Position HeroPosition { get; set; } = Position.Button;

    public Dictionary<Position, double> StartingStacks { get; set; } = [];

    public string? HeroCards { get; set; }

    public string Board { get; set; } = string.Empty;

    public List<StoredAction> Actions { get; set; } = [];
}
