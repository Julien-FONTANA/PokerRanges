using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using PokerRanges.Core.Preflop;

namespace PokerRanges.Data;

public sealed class JsonPreflopChartRepository : IPreflopChartRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly PreflopChartRepositoryOptions _options;
    private readonly ILogger<JsonPreflopChartRepository> _logger;
    private readonly ConcurrentDictionary<PreflopChart, RangeStrategy> _strategies = new();

    private IReadOnlyList<PreflopChart> _charts = [];

    public JsonPreflopChartRepository(
        PreflopChartRepositoryOptions options,
        ILogger<JsonPreflopChartRepository> logger)
    {
        _options = options;
        _logger = logger;

        ExtractDefaults(overwriteExisting: false);
        Reload();
    }

    public IReadOnlyList<PreflopChart> Charts => _charts;

    public string? EditableDirectory => _options.UserChartsDirectory;

    public ChartResolution Resolve(ChartKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return ChartResolver.Resolve(_charts, key, StrategyFor);
    }

    public int RestoreDefaults()
    {
        int written = ExtractDefaults(overwriteExisting: true);
        Reload();

        return written;
    }

    public void Reload()
    {
        _strategies.Clear();

        Dictionary<ChartKey, PreflopChart> byKey = [];

        foreach (PreflopChart chart in LoadEmbedded())
        {
            byKey[KeyOf(chart)] = chart;
        }

        int userChartCount = 0;
        foreach (PreflopChart chart in LoadUserCharts())
        {
            byKey[KeyOf(chart)] = chart;
            userChartCount++;
        }

        _charts = [.. byKey.Values];

        _logger.LogInformation(
            "{Total} preflop charts loaded, {UserCount} of them from {Directory}.",
            _charts.Count,
            userChartCount,
            _options.UserChartsDirectory ?? "(no user directory)");
    }

    /// <summary>
    /// Copies the shipped charts into the editable directory. Without overwriting on first run —
    /// that is what gives the user something to edit; with overwriting when they ask to restore,
    /// which lets them break a range without fear.
    /// </summary>
    private int ExtractDefaults(bool overwriteExisting)
    {
        string? directory = _options.UserChartsDirectory;

        if (string.IsNullOrWhiteSpace(directory))
        {
            return 0;
        }

        Assembly assembly = typeof(JsonPreflopChartRepository).Assembly;
        int written = 0;

        try
        {
            Directory.CreateDirectory(directory);

            foreach (string name in EmbeddedChartNames(assembly))
            {
                string path = Path.Combine(directory, FileNameOf(name));

                if (!overwriteExisting && File.Exists(path))
                {
                    continue;
                }

                using Stream? stream = assembly.GetManifestResourceStream(name);
                if (stream is null)
                {
                    continue;
                }

                using FileStream target = File.Create(path);
                stream.CopyTo(target);
                written++;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(
                exception,
                "Could not write the shipped charts to {Directory}: falling back on the embedded ones.",
                directory);

            return written;
        }

        if (written > 0)
        {
            _logger.LogInformation("{Count} shipped charts written to {Directory}.", written, directory);
        }

        return written;
    }

    private static IEnumerable<string> EmbeddedChartNames(Assembly assembly)
    {
        return assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The resource name carries the full namespace; only the last two segments are kept, which is
    /// the file name as it appears in the repository.
    /// </summary>
    private static string FileNameOf(string resourceName)
    {
        string[] segments = resourceName.Split('.');

        return segments.Length < 2
            ? resourceName
            : string.Join('.', segments[^2], segments[^1]);
    }

    private static ChartKey KeyOf(PreflopChart chart)
    {
        return new ChartKey(chart.Context, chart.PlayersLeftToAct, chart.Relation, chart.DepthInBigBlinds);
    }

    private RangeStrategy StrategyFor(PreflopChart chart)
    {
        return _strategies.GetOrAdd(chart, RangeStrategy.FromChart);
    }

    private IEnumerable<PreflopChart> LoadEmbedded()
    {
        Assembly assembly = typeof(JsonPreflopChartRepository).Assembly;

        foreach (string name in EmbeddedChartNames(assembly))
        {
            using Stream? stream = assembly.GetManifestResourceStream(name);
            if (stream is null)
            {
                continue;
            }

            foreach (PreflopChart chart in ReadDocument(stream, name))
            {
                yield return chart;
            }
        }
    }

    private IEnumerable<PreflopChart> LoadUserCharts()
    {
        string? directory = _options.UserChartsDirectory;

        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            yield break;
        }

        foreach (string path in Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories))
        {
            using FileStream stream = File.OpenRead(path);

            foreach (PreflopChart chart in ReadDocument(stream, path))
            {
                yield return chart;
            }
        }
    }

    private List<PreflopChart> ReadDocument(Stream stream, string origin)
    {
        ChartDocument? document;

        try
        {
            document = JsonSerializer.Deserialize<ChartDocument>(stream, SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new PreflopChartException($"Chart file \"{origin}\" is not valid JSON.", exception);
        }

        if (document is null)
        {
            throw new PreflopChartException($"Chart file \"{origin}\" is empty.");
        }

        return
        [
            .. document.Charts.Select(entry => new PreflopChart
            {
                Context = entry.Context,
                PlayersLeftToAct = entry.PlayersLeftToAct,
                Relation = entry.Relation,
                DepthInBigBlinds = entry.DepthInBigBlinds,
                Source = document.Source,
                Actions =
                [
                    .. entry.Actions.Select(action => new ChartAction
                    {
                        Kind = action.Kind,
                        SizeInBigBlinds = action.SizeInBigBlinds,
                        Range = action.Range,
                    }),
                ],
            }),
        ];
    }
}
