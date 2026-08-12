using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using PokerRanges.Core.Cards;
using PokerRanges.Core.Equity;
using PokerRanges.Core.Evaluation;
using PokerRanges.Core.Ranges;

namespace PokerRanges.Core.Tests.Ranges;

/// <summary>
/// Regenerates the table embedded in <see cref="PreflopHandStrength"/>. Equity against a random hand
/// is a fixed constant of the game, so it is measured once here and shipped as data rather than
/// recomputed on every launch — at the sample count needed to order neighbouring hands correctly,
/// measuring it would cost minutes.
/// </summary>
public sealed class PreflopHandStrengthGenerator
{
    /// <summary>
    /// Deliberately not <see cref="PokerRanges.Core.Postflop.PostflopOptions.RandomSeed"/>: class
    /// indices 0-168 would collide with the per-combo streams the range ranker already derives from
    /// it.
    /// </summary>
    private const int SeedBase = 20260812;

    private const int SamplesPerClass = 400_000;

    /// <summary>
    /// Explicit: it is a tool, not a check. Run it only to rewrite the table, then paste the file it
    /// writes into <see cref="PreflopHandStrength"/>.
    /// </summary>
    [Fact(Explicit = true)]
    public async Task Regenerate()
    {
        RankCountHandEvaluator evaluator = new();
        EquityCalculator calculator = new(evaluator, NullLogger<EquityCalculator>.Instance);

        (HandClass HandClass, double Equity)[] measured = new (HandClass, double)[HandClass.Count];

        await Parallel.ForEachAsync(
            Enumerable.Range(0, HandClass.Count),
            TestContext.Current.CancellationToken,
            async (index, cancellationToken) =>
            {
                HandClass handClass = HandClass.All[index];

                // Every combo of a class has the same equity by suit symmetry, so one representative
                // answers for all of them.
                HoleCards representative = handClass.Combos().First();

                EquityResult result = await calculator.ComputeAsync(
                    new EquityRequest
                    {
                        PlayerRanges =
                        [
                            new HandRangeBuilder().Set(representative, 1).Build(),
                            HandRange.Full,
                        ],
                        Method = EquityMethod.MonteCarlo,
                        MaximumSamples = SamplesPerClass,
                        TargetStandardError = 0,
                        RandomSeed = SeedBase + index,
                    },
                    cancellationToken);

                measured[index] = (handClass, result.Hero.Equity);
            });

        StringBuilder table = new();
        foreach ((HandClass handClass, double equity) in measured
            .OrderByDescending(entry => entry.Equity)
            .ThenBy(entry => entry.HandClass.GridRow)
            .ThenBy(entry => entry.HandClass.GridColumn))
        {
            table.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"        new(\"{handClass}\", {equity:0.0000}),"));
        }

        string path = Path.Combine(Path.GetTempPath(), "preflop-hand-strength.txt");
        await File.WriteAllTextAsync(path, table.ToString(), TestContext.Current.CancellationToken);

        Assert.True(File.Exists(path), path);
    }
}
