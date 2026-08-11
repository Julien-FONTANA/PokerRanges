using Avalonia;
using PokerRanges.Core.Localization;

namespace PokerRanges.App;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // L'anglais avant tout le reste : sans cela l'application démarrerait dans la langue du
        // système, et le premier écran serait déjà écrit avant que les réglages ne soient relus.
        Language.Use(AppLanguage.English);

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
