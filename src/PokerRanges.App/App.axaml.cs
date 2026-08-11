using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PokerRanges.App.Infrastructure;
using PokerRanges.App.ViewModels;
using PokerRanges.App.Views;

namespace PokerRanges.App;

public sealed partial class App : Application
{
    private ServiceProvider? _services;
    private MainWindowViewModel? _viewModel;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ServiceCollection services = new();
        services.AddPokerRangesApp();
        _services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _viewModel = _services.GetRequiredService<MainWindowViewModel>();

            // « --compact » ouvre directement la fenêtre réduite et épinglée : c'est la forme utile
            // quand le raccourci de lancement sert à assister une partie déjà commencée. L'option
            // force le mode compact, elle ne le retire jamais à qui l'a déjà choisi.
            if (desktop.Args?.Contains("--compact", StringComparer.OrdinalIgnoreCase) == true)
            {
                _viewModel.IsCompact = true;
            }

            desktop.MainWindow = new MainWindow { DataContext = _viewModel };
            desktop.ShutdownRequested += OnShutdownRequested;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        // La fermeture est le dernier moment où la main en cours peut encore être écrite : l'écriture
        // ordinaire est différée, et une main saisie juste avant de quitter serait perdue sans ceci.
        _viewModel?.PersistNow();
        _viewModel = null;

        _services?.Dispose();
        _services = null;
    }
}
