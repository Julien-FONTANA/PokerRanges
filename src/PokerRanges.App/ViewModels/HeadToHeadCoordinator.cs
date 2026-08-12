using Microsoft.Extensions.Logging;
using PokerRanges.Core;
using PokerRanges.Core.HeadToHead;

namespace PokerRanges.App.ViewModels;

/// <summary>
/// Turns a head-to-head request into a result, debounced and cancelling the request in flight:
/// otherwise every cell clicked would start a Monte-Carlo run nobody is waiting for.
/// </summary>
/// <remarks>
/// No <c>ConfigureAwait(false)</c> anywhere on purpose. A bare <c>await</c> captures Avalonia's
/// synchronization context in the running application, so the bound properties below are written on
/// the UI thread, and captures nothing under the tests, which is what lets the view models be driven
/// without a UI thread at all.
/// </remarks>
public sealed class HeadToHeadCoordinator
{
    private readonly IHeadToHeadCalculator _calculator;
    private readonly ILogger<HeadToHeadCoordinator> _logger;

    private CancellationTokenSource? _pending;

    public HeadToHeadCoordinator(IHeadToHeadCalculator calculator, ILogger<HeadToHeadCoordinator> logger)
    {
        _calculator = calculator;
        _logger = logger;
    }

    public HeadToHeadResultViewModel Result { get; } = new();

    /// <summary>There is nothing to compare: a side is empty or the board is half typed.</summary>
    public void ShowProblem(string message)
    {
        _pending?.Cancel();
        _pending = null;

        Result.ShowWaiting(message);
        Result.IsBusy = false;
    }

    public async Task ShowAsync(HeadToHeadRequest request, int delayMilliseconds)
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

            Result.IsBusy = true;

            // The equity calculator already moves its own work off this thread.
            HeadToHeadResult result = await _calculator.ComputeAsync(request, cancellationToken);

            Result.Show(result);
        }
        catch (OperationCanceledException)
        {
            // A more recent entry has taken over.
        }
        catch (PokerRangesException exception)
        {
            _logger.LogWarning(exception, "Head-to-head impossible: {Message}", exception.Message);
            Result.ShowWaiting(exception.Message);
        }
        finally
        {
            Result.IsBusy = false;
        }
    }
}
