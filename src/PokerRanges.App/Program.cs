using Avalonia;
using PokerRanges.Core.Localization;

namespace PokerRanges.App;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // English before anything else: without this the application would start in the system
        // language, and the first screen would be drawn before the settings are read back.
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
