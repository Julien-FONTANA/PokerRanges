using Microsoft.Extensions.Logging.Abstractions;
using PokerRanges.Core.Preflop;
using Shouldly;

namespace PokerRanges.Data.Tests;

/// <summary>
/// The shipped charts are copied into an editable directory, and the user can modify them. What
/// makes that editing acceptable is being able to go back: without restore, a broken range would
/// stay broken until reinstallation.
/// </summary>
public sealed class EditableChartsTests : IDisposable
{
    private readonly string _directory;
    private readonly PreflopChartRepositoryOptions _options;

    public EditableChartsTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "PokerRanges.Tests", Guid.NewGuid().ToString("N"));

        _options = new PreflopChartRepositoryOptions { UserChartsDirectory = _directory };
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void TheFirstStartLeavesTheDeliveredChartsWhereTheUserCanEditThem()
    {
        JsonPreflopChartRepository repository = NewRepository();

        string[] files = Directory.GetFiles(_directory, "*.json");

        files.ShouldNotBeEmpty();
        repository.EditableDirectory.ShouldBe(_directory);
        repository.Charts.Count.ShouldBe(29);
    }

    /// <summary>
    /// The editable directory is the user's source of truth: a chart they have modified must not
    /// be overwritten on the next launch, or their settings would vanish silently.
    /// </summary>
    [Fact]
    public void AnEditedChartIsNotOverwrittenAtTheNextStart()
    {
        NewRepository();

        string edited = Directory.GetFiles(_directory, "*.json")[0];
        string mine = ReplaceEveryRangeWithAcesOnly(File.ReadAllText(edited));
        File.WriteAllText(edited, mine);

        NewRepository();

        File.ReadAllText(edited).ShouldBe(mine);
    }

    [Fact]
    public void RestoringPutsTheDeliveredChartsBackAndReloadsThem()
    {
        JsonPreflopChartRepository repository = NewRepository();

        string edited = Directory.GetFiles(_directory, "*.json")[0];
        string original = File.ReadAllText(edited);
        File.WriteAllText(edited, ReplaceEveryRangeWithAcesOnly(original));
        repository.Reload();

        int written = repository.RestoreDefaults();

        written.ShouldBeGreaterThan(0);
        File.ReadAllText(edited).ShouldBe(original);
        repository.Charts.Count.ShouldBe(29);
    }

    [Fact]
    public void AnEditedRangeReallyChangesTheAdviceOnceReloaded()
    {
        JsonPreflopChartRepository repository = NewRepository();
        ChartKey key = new(PreflopContext.RaiseFirstIn, 2, FacingRelation.InPosition, 100);

        double before = repository.Resolve(key).Strategy.RangeOf(ChartActionKind.Raise).TotalCombos;

        foreach (string path in Directory.GetFiles(_directory, "*.json"))
        {
            File.WriteAllText(path, ReplaceEveryRangeWithAcesOnly(File.ReadAllText(path)));
        }

        repository.Reload();

        repository.Resolve(key).Strategy.RangeOf(ChartActionKind.Raise).TotalCombos.ShouldBeLessThan(before);

        repository.RestoreDefaults();

        repository.Resolve(key).Strategy.RangeOf(ChartActionKind.Raise).TotalCombos.ShouldBe(before);
    }

    [Fact]
    public void WithoutAnEditableFolderOnlyTheDeliveredChartsAreUsed()
    {
        JsonPreflopChartRepository repository = new(
            PreflopChartRepositoryOptions.EmbeddedOnly,
            NullLogger<JsonPreflopChartRepository>.Instance);

        repository.EditableDirectory.ShouldBeNull();
        repository.RestoreDefaults().ShouldBe(0);
        repository.Charts.Count.ShouldBe(29);
    }

    /// <summary>
    /// Cuts every range in the file down to "AA": a tiny edit to write, but one whose effect on
    /// the combo count cannot be mistaken for noise.
    /// </summary>
    private static string ReplaceEveryRangeWithAcesOnly(string json)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            json,
            "\"range\"\\s*:\\s*\"[^\"]*\"",
            "\"range\": \"AA\"");
    }

    private JsonPreflopChartRepository NewRepository()
    {
        return new JsonPreflopChartRepository(_options, NullLogger<JsonPreflopChartRepository>.Instance);
    }
}
