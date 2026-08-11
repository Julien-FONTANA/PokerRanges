using System.Globalization;
using Avalonia.Media;
using PokerRanges.App.Rendering;
using PokerRanges.Core.Postflop;

namespace PokerRanges.App.ViewModels;

public sealed record ActionEvaluationViewModel(
    string Label,
    string ExpectedValue,
    string Detail,
    IBrush Brush,
    bool IsBest,
    double BarWidth)
{
    private const double MaximumBarWidth = 150;

    public FontWeight FontWeight => IsBest ? FontWeight.Bold : FontWeight.Normal;

    public static IReadOnlyList<ActionEvaluationViewModel> From(PostflopAdvice advice, double bigBlind)
    {
        ArgumentNullException.ThrowIfNull(advice);

        double scale = 0;
        foreach (ActionEvaluation candidate in advice.Candidates)
        {
            scale = Math.Max(scale, Math.Abs(candidate.ExpectedValue));
        }

        List<ActionEvaluationViewModel> rows = [];

        foreach (ActionEvaluation candidate in advice.Candidates)
        {
            rows.Add(new ActionEvaluationViewModel(
                candidate.Label,
                string.Create(
                    CultureInfo.CurrentCulture,
                    $"{candidate.ExpectedValueInBigBlinds(bigBlind):+0.0;-0.0;0.0}bb"),
                candidate.Explanation,
                ActionPalette.BrushOf(candidate.Kind),
                ReferenceEquals(candidate, advice.Best),
                scale <= 0 ? 2 : Math.Max(2, Math.Abs(candidate.ExpectedValue) / scale * MaximumBarWidth)));
        }

        return rows;
    }
}
