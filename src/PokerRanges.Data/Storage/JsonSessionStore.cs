using Microsoft.Extensions.Logging;
using PokerRanges.Core;
using PokerRanges.Core.Session;
using PokerRanges.Core.Table;

namespace PokerRanges.Data.Storage;

/// <summary>
/// Réglages et main en cours, en JSON sous le profil utilisateur.
/// </summary>
public sealed class JsonSessionStore : ISessionStore
{
    private const string PreferencesLabel = "les réglages";
    private const string HandLabel = "la main en cours";

    private readonly SessionStoreOptions _options;
    private readonly ILogger<JsonSessionStore> _logger;

    public JsonSessionStore(SessionStoreOptions options, ILogger<JsonSessionStore> logger)
    {
        _options = options;
        _logger = logger;
    }

    public UserPreferences LoadPreferences()
    {
        StoredPreferences? stored = JsonFileStore.Read<StoredPreferences>(
            _options.PreferencesFilePath,
            PreferencesLabel,
            _logger);

        if (stored is null)
        {
            return UserPreferences.Default;
        }

        return new UserPreferences
        {
            PlayerCount = stored.PlayerCount,
            BigBlind = stored.BigBlind,
            StartingStack = stored.StartingStack,
            AnteStyle = stored.AnteStyle,
            AnteAmount = stored.AnteAmount,
            HeroPosition = stored.HeroPosition,
            OpponentProfile = stored.OpponentProfile,
            PrefersCompactLayout = stored.PrefersCompactLayout,
            Language = stored.Language,
        };
    }

    public void SavePreferences(UserPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        JsonFileStore.Write(
            _options.PreferencesFilePath,
            new StoredPreferences
            {
                PlayerCount = preferences.PlayerCount,
                BigBlind = preferences.BigBlind,
                StartingStack = preferences.StartingStack,
                AnteStyle = preferences.AnteStyle,
                AnteAmount = preferences.AnteAmount,
                HeroPosition = preferences.HeroPosition,
                OpponentProfile = preferences.OpponentProfile,
                PrefersCompactLayout = preferences.PrefersCompactLayout,
                Language = preferences.Language,
            },
            PreferencesLabel,
            _logger);
    }

    public HandState? LoadHand()
    {
        StoredHand? stored = JsonFileStore.Read<StoredHand>(_options.HandFilePath, HandLabel, _logger);

        if (stored is null)
        {
            return null;
        }

        try
        {
            return StoredHandMapper.ToHandState(stored);
        }
        catch (PokerRangesException exception)
        {
            _logger.LogWarning(
                exception,
                "La main enregistrée dans {Path} n'est pas exploitable, elle est ignorée.",
                _options.HandFilePath);

            return null;
        }
    }

    public void SaveHand(HandState? hand)
    {
        if (hand is null)
        {
            JsonFileStore.Delete(_options.HandFilePath, HandLabel, _logger);
            return;
        }

        JsonFileStore.Write(_options.HandFilePath, StoredHandMapper.ToStored(hand), HandLabel, _logger);
    }
}
