using System.ComponentModel;
using Avalonia.Controls;
using PokerRanges.App.ViewModels;

namespace PokerRanges.App.Views;

/// <summary>
/// The shell that carries the shortcuts and arbitrates between the two layouts. Resizing lives
/// here rather than in the view model: a window's size is a window's business.
/// </summary>
public sealed partial class MainWindow : Window
{
    // The card grid sets the floor: thirteen columns need their width, four rows their height, and
    // below that the advice is what gets squeezed out.
    private const double CompactWidth = 470;
    private const double CompactHeight = 580;
    private const double CompactMinWidth = 420;
    private const double CompactMinHeight = 500;
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

            // Lower bounds first: setting the width before them would have it clamped.
            MinWidth = CompactMinWidth;
            MinHeight = CompactMinHeight;
            Width = CompactWidth;
            Height = CompactHeight;
            return;
        }

        MinWidth = AnalysisMinWidth;
        MinHeight = AnalysisMinHeight;
        Width = Math.Max(_analysisWidth, AnalysisMinWidth);
        Height = Math.Max(_analysisHeight, AnalysisMinHeight);
    }
}
