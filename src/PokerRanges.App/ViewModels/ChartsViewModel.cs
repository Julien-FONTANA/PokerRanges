using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PokerRanges.App.Localization;
using PokerRanges.Core.Preflop;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// The chart library as handled from the interface. Editing a range by hand only makes sense if
/// the effect can be seen without restarting the application, and undone without reinstalling it:
/// hence the reload / restore pair.
/// </summary>
public sealed partial class ChartsViewModel : ObservableObject
{
    private readonly IPreflopChartRepository _charts;
    private readonly ILogger<ChartsViewModel> _logger;

    [ObservableProperty]
    private string _status = string.Empty;

    public ChartsViewModel(IPreflopChartRepository charts, ILogger<ChartsViewModel> logger)
    {
        _charts = charts;
        _logger = logger;

        Describe();
    }

    /// <summary>The charts have changed: the advice on screen is out of date.</summary>
    public event EventHandler? Changed;

    public UiText Text => UiText.Current;

    public string? EditableDirectory => _charts.EditableDirectory;

    /// <summary>Rebuilds the summary after a language change.</summary>
    public void Refresh()
    {
        Describe();
    }

    [RelayCommand]
    public void Reload()
    {
        _charts.Reload();
        Describe();

        _logger.LogInformation("Charts rechargés à la demande.");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void Restore()
    {
        int written = _charts.RestoreDefaults();
        Describe();

        Status = UiMatrixText.ChartsRestored(written, _charts.Charts.Count);

        _logger.LogInformation("{Count} charts restaurés dans leur version livrée.", written);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Describe()
    {
        Status = _charts.EditableDirectory is string directory
            ? UiMatrixText.ChartsStatus(_charts.Charts.Count, directory)
            : UiMatrixText.ChartsEmbeddedOnly(_charts.Charts.Count);
    }
}
