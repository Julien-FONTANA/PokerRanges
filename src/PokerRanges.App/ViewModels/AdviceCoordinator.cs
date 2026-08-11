using Microsoft.Extensions.Logging;
using PokerRanges.App.Localization;
using PokerRanges.Core;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Localization;
using PokerRanges.Core.Postflop;
using PokerRanges.Core.Preflop;
using PokerRanges.Core.Table;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// Traduit un état de main en grille et en recommandation. Tout ce qui coûte part hors du fil
/// d'affichage, avec anti-rebond et annulation de la requête précédente : à la saisie, chaque carte
/// tapée déclencherait sinon un Monte-Carlo dont personne n'attend le résultat.
/// </summary>
public sealed class AdviceCoordinator
{
    private readonly IPreflopAdvisor _preflopAdvisor;
    private readonly IPostflopAdvisor _postflopAdvisor;
    private readonly ILogger<AdviceCoordinator> _logger;

    private CancellationTokenSource? _pending;

    public AdviceCoordinator(
        IPreflopAdvisor preflopAdvisor,
        IPostflopAdvisor postflopAdvisor,
        ILogger<AdviceCoordinator> logger)
    {
        _preflopAdvisor = preflopAdvisor;
        _postflopAdvisor = postflopAdvisor;
        _logger = logger;
    }

    public RangeMatrixViewModel Matrix { get; } = new();

    public RecommendationViewModel Recommendation { get; } = new();

    /// <summary>Il n'y a rien à conseiller : la saisie est incomplète ou incohérente.</summary>
    public void ShowProblem(string message)
    {
        _pending?.Cancel();
        _pending = null;

        Matrix.ShowNothing(message);
        Recommendation.ShowWaiting(message);
        Recommendation.IsBusy = false;
    }

    public async Task ShowAsync(AdviceRequest request, int delayMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(request);

        _pending?.Cancel();
        CancellationTokenSource current = new();
        _pending = current;
        CancellationToken cancellationToken = current.Token;

        try
        {
            if (delayMilliseconds > 0)
            {
                await Task.Delay(delayMilliseconds, cancellationToken);
            }

            await AdviseAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Une saisie plus récente a pris la main.
        }
        catch (PokerRangesException exception)
        {
            _logger.LogWarning(exception, "Conseil impossible : {Message}", exception.Message);
            Matrix.ShowNothing(exception.Message);
            Recommendation.ShowWaiting(exception.Message);
        }
        finally
        {
            Recommendation.IsBusy = false;
        }
    }

    private async Task AdviseAsync(AdviceRequest request, CancellationToken cancellationToken)
    {
        HandState state = request.State;
        TableConfiguration table = state.Table;

        if (state.Actions.Any(action => action.Position == table.HeroPosition && action.Kind == PlayerActionKind.Fold))
        {
            ShowProblem(UiText.Current.YouFolded);
            return;
        }

        if (request.Analysis.Street == Street.Preflop)
        {
            ShowPreflop(request);
            return;
        }

        if (state.HeroCards is null)
        {
            ShowProblem(UiText.Current.PickTwoCardsPostflop);
            return;
        }

        Recommendation.IsBusy = true;

        PostflopAdvice advice = await Task.Run(
            () => _postflopAdvisor.AdviseAsync(state, request.Profile, request.Budget, cancellationToken),
            cancellationToken);

        OpponentRange shown = advice.Opponents[0];
        HoleCards hero = state.HeroCards.Value;

        Matrix.ShowRange(
            shown.Range,
            hero.ToHandClass(),
            [hero.First, hero.Second, .. state.Board],
            PositionLayout.Describe(shown.Position),
            UiMatrixText.OpponentRangeTitle(
                PositionLayout.Describe(shown.Position),
                TableText.Describe(request.Analysis.Street)),
            advice.Board.Describe() + UiMatrixText.CombosSuffix(shown.Combos));

        Recommendation.Show(advice, table.BigBlind);
    }

    private void ShowPreflop(AdviceRequest request)
    {
        HandState state = request.State;
        ChartResolution resolution = _preflopAdvisor.ResolveChart(state);
        PreflopSituation situation = PreflopSituationReader.Read(
            state,
            request.Analysis,
            PreflopAdvisorOptions.Default.JamThresholdInBigBlinds);

        Matrix.Show(
            resolution.Strategy,
            state.HeroCards?.ToHandClass(),
            UiMatrixText.PreflopGridTitle(
                PreflopContextLabels.Describe(situation.Context),
                PositionLayout.Describe(state.Table.HeroPosition),
                situation.DepthInBigBlinds),
            resolution.Describe());

        if (state.HeroCards is null)
        {
            Recommendation.ShowWaiting(UiText.Current.PickTwoCards);
            return;
        }

        Recommendation.Show(_preflopAdvisor.Advise(state));

        if (request.Analysis.NextToAct is Position next && next != state.Table.HeroPosition)
        {
            Recommendation.Problem = UiMatrixText.NotYourTurn(PositionLayout.Describe(next));
        }
    }
}
