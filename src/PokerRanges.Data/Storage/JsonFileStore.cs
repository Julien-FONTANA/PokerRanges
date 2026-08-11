using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace PokerRanges.Data.Storage;

/// <summary>
/// The file policy shared by all session data: lenient reads, atomic writes.
/// <para>
/// A read never throws — a missing, truncated or tampered file counts as "nothing saved", because
/// refusing to start because of one's own resume file is worse than forgetting everything.
/// A write goes through a renamed temporary file: killing the application mid-save must leave the
/// previous version intact, never a half-written file.
/// </para>
/// </summary>
internal static class JsonFileStore
{
    public static JsonSerializerOptions Serializer { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static T? Read<T>(string path, string what, ILogger logger)
        where T : class
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using FileStream stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<T>(stream, Serializer);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not read {What} back from {Path}, starting fresh.", what, path);
            return null;
        }
    }

    public static void Write<T>(string path, T value, string what, ILogger logger)
    {
        try
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(value, Serializer));
            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not save {What} to {Path}.", what, path);
        }
    }

    public static void Delete(string path, string what, ILogger logger)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not delete {What} at {Path}.", what, path);
        }
    }
}
