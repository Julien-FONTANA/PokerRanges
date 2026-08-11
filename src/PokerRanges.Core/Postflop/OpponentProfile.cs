using System.Globalization;
using PokerRanges.Core.Localization;

namespace PokerRanges.Core.Postflop;

/// <summary>
/// Comment l'adversaire réagit à une mise. <see cref="DefenceFactor"/> multiplie la fréquence de
/// défense minimale : 1 signifie qu'il défend exactement ce qu'il faut pour ne pas être exploité,
/// en dessous il se couche trop, au dessus il paie trop.
/// <para>
/// Le nom se lit à travers <see cref="Name"/> et suit donc la langue courante ; l'identité d'un
/// profil tient à sa référence, jamais à son libellé, sous peine de la perdre à la traduction.
/// </para>
/// </summary>
public sealed record OpponentProfile
{
    private readonly Func<string> _name;

    private OpponentProfile(
        Func<string> name,
        double defenceFactor,
        double raiseFraction,
        double bettingFraction,
        double bluffFraction)
    {
        _name = name;
        DefenceFactor = defenceFactor;
        RaiseFraction = raiseFraction;
        BettingFraction = bettingFraction;
        BluffFraction = bluffFraction;
    }

    public string Name => _name();

    public double DefenceFactor { get; }

    public double RaiseFraction { get; }

    public double BettingFraction { get; }

    public double BluffFraction { get; }

    public static OpponentProfile Balanced { get; } =
        new(() => PostflopText.ProfileBalanced, 1.00, 0.15, 0.50, 0.15);

    public static OpponentProfile Tight { get; } =
        new(() => PostflopText.ProfileTight, 0.75, 0.10, 0.35, 0.05);

    public static OpponentProfile CallingStation { get; } =
        new(() => PostflopText.ProfileCallingStation, 1.35, 0.05, 0.40, 0.05);

    public static OpponentProfile Aggressive { get; } =
        new(() => PostflopText.ProfileAggressive, 1.05, 0.30, 0.70, 0.30);

    public static IReadOnlyList<OpponentProfile> All { get; } =
        [Balanced, Tight, CallingStation, Aggressive];

    /// <summary>
    /// Retrouve un profil par son nom dans n'importe quelle langue : les réglages enregistrés en
    /// français doivent continuer d'ouvrir sur le bon profil une fois l'interface passée en anglais.
    /// </summary>
    public static OpponentProfile? Find(string name)
    {
        foreach (OpponentProfile profile in All)
        {
            if (Matches(profile, name))
            {
                return profile;
            }
        }

        return null;
    }

    private static bool Matches(OpponentProfile profile, string name)
    {
        foreach (AppLanguage language in (AppLanguage[])[AppLanguage.English, AppLanguage.French])
        {
            if (NameIn(profile, language).Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string NameIn(OpponentProfile profile, AppLanguage language)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = Language.CultureOf(language);

        try
        {
            return profile.Name;
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
