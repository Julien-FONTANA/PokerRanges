namespace PokerRanges.App.Infrastructure;

/// <summary>
/// The application's storage locations, all under the user profile. Charts live in Roaming
/// (editable and worth keeping), logs in Local.
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

    /// <summary>The interrupted hand: to resume on next launch, not to keep.</summary>
    public static string HandFilePath { get; } = Path.Combine(LocalRoot, "hand-in-progress.json");

    public static string JournalFilePath { get; } = Path.Combine(RoamingRoot, "journal.json");

    public static string LogDirectory { get; } = Path.Combine(LocalRoot, "logs");

    public static string LogFilePath { get; } = Path.Combine(LogDirectory, "pokerranges.log");
}
