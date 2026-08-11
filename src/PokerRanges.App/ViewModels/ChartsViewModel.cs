using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PokerRanges.App.Localization;
using PokerRanges.Core.Preflop;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// La bibliothèque de charts, telle qu'on la manipule depuis l'interface. Éditer une range à la
/// main n'a de sens que si l'on peut voir l'effet sans relancer l'application, et revenir en
/// arrière sans la réinstaller : d'où le couple recharger / restaurer.
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

    /// <summary>Les charts ont changé : le conseil affiché n'est plus à jour.</summary>
    public event EventHandler? Changed;

    public UiText Text => UiText.Current;

    public string? EditableDirectory => _charts.EditableDirectory;

    /// <summary>Reconstruit le résumé après un changement de langue.</summary>
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
