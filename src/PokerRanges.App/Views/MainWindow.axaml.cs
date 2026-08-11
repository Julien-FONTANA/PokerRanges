using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using PokerRanges.App.ViewModels;

namespace PokerRanges.App.Views;

/// <summary>
/// La coque qui porte les raccourcis et arbitre entre les deux dispositions. Le redimensionnement
/// vit ici plutôt que dans le modèle de vue : la taille d'une fenêtre est une affaire de fenêtre.
/// </summary>
public sealed partial class MainWindow : Window
{
    private const double CompactWidth = 470;
    private const double CompactHeight = 560;
    private const double CompactMinWidth = 380;
    private const double CompactMinHeight = 460;
    private const double AnalysisMinWidth = 1120;
    private const double AnalysisMinHeight = 760;

    private MainWindowViewModel? _viewModel;
    private double _analysisWidth = 1440;
    private double _analysisHeight = 960;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs args)
    {
        base.OnDataContextChanged(args);

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelChanged;
        }

        _viewModel = DataContext as MainWindowViewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelChanged;
            ApplyLayout(_viewModel.IsCompact);
        }
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(MainWindowViewModel.IsCompact) && _viewModel is not null)
        {
            ApplyLayout(_viewModel.IsCompact);
        }
    }

    private void ApplyLayout(bool compact)
    {
        if (compact)
        {
            _analysisWidth = Width;
            _analysisHeight = Height;

            // Les bornes basses d'abord : poser la largeur avant elles la ferait rogner.
            MinWidth = CompactMinWidth;
            MinHeight = CompactMinHeight;
            Width = CompactWidth;
            Height = CompactHeight;

            Dispatcher.UIThread.Post(Compact.FocusEntry, DispatcherPriority.Input);
            return;
        }

        MinWidth = AnalysisMinWidth;
        MinHeight = AnalysisMinHeight;
        Width = Math.Max(_analysisWidth, AnalysisMinWidth);
        Height = Math.Max(_analysisHeight, AnalysisMinHeight);
    }
}
