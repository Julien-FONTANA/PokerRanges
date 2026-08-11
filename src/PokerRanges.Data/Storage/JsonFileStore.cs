using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace PokerRanges.Data.Storage;

/// <summary>
/// La politique de fichier commune aux données de session : lecture tolérante, écriture atomique.
/// <para>
/// Une lecture ne lève jamais — un fichier absent, tronqué ou trafiqué vaut « rien de sauvegardé »,
/// car refuser de démarrer à cause de son propre fichier de reprise est pire que tout oublier.
/// Une écriture passe par un temporaire renommé : couper l'application en plein enregistrement doit
/// laisser la version précédente intacte, jamais un fichier à moitié écrit.
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
            logger.LogWarning(exception, "Impossible de relire {What} depuis {Path}, on repart à neuf.", what, path);
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
            logger.LogWarning(exception, "Impossible d'enregistrer {What} dans {Path}.", what, path);
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
            logger.LogWarning(exception, "Impossible d'effacer {What} dans {Path}.", what, path);
        }
    }
}
