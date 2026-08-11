namespace PokerRanges.App.Infrastructure;

/// <summary>
/// Emplacements de stockage de l'application, tous sous le profil utilisateur.
/// Les charts sont dans Roaming (éditables et à conserver), les journaux dans Local.
/// </summary>
public static class AppPaths
{
    private const string ApplicationFolderName = "PokerRanges";

    public static string RoamingRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        ApplicationFolderName);

    public static string LocalRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ApplicationFolderName);

    public static string ChartsDirectory { get; } = Path.Combine(RoamingRoot, "charts");

    public static string SettingsFilePath { get; } = Path.Combine(RoamingRoot, "settings.json");

    /// <summary>La main interrompue : à reprendre au prochain lancement, pas à conserver.</summary>
    public static string HandFilePath { get; } = Path.Combine(LocalRoot, "hand-in-progress.json");

    public static string JournalFilePath { get; } = Path.Combine(RoamingRoot, "journal.json");

    public static string LogDirectory { get; } = Path.Combine(LocalRoot, "logs");

    public static string LogFilePath { get; } = Path.Combine(LogDirectory, "pokerranges.log");
}
