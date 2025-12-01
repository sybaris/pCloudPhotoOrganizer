using System.Text.Json;

namespace pCloudPhotoOrganizer.Services;

public class SettingsService
{
    private const string KeyFolders = "media_folders";

    /// <summary>
    /// Retourne la liste des dossiers configurés pour les photos.
    /// </summary>
    public List<string> GetSelectedFolders()
    {
        var json = Preferences.Default.Get(KeyFolders, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            // Si le JSON est corrompu, on repart à zéro.
            return new List<string>();
        }
    }

    /// <summary>
    /// Sauvegarde la liste des dossiers configurés.
    /// </summary>
    public void SaveSelectedFolders(IEnumerable<string> folders)
    {
        var list = folders
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Distinct()
            .ToList();

        var json = JsonSerializer.Serialize(list);
        Preferences.Default.Set(KeyFolders, json);
    }

    /// <summary>
    /// Indique si au moins un dossier a été configuré.
    /// </summary>
    public bool AreFoldersConfigured()
        => GetSelectedFolders().Any();

    /// <summary>
    /// Réinitialise la configuration des dossiers.
    /// </summary>
    public void ClearFolders()
    {
        Preferences.Default.Remove(KeyFolders);
    }
}
