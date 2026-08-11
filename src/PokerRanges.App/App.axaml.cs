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

            // "--compact" opens straight into the reduced, pinned window: the useful shape when
            // the launch shortcut is there to assist a game already under way. The option forces
            // compact mode on; it never takes it away from someone who already chose it.
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
        // Shutdown is the last moment the hand in progress can still be written: the ordinary
        // write is deferred, and a hand entered just before quitting would be lost without this.
        _viewModel?.PersistNow();
        _viewModel = null;

        _services?.Dispose();
        _services = null;
    }
}
