using System.Text.Json;
using System.Threading.Tasks;

namespace pCloudPhotoOrganizer.Services;

public class SettingsService
{
    private const string KeyFolders = "media_folders";
    private const string KeyPCloudUser = "pcloud_user";
    private const string KeyPCloudRoot = "pcloud_root";
    private const string KeyPCloudToken = "pcloud_token";

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

    public string? GetPCloudUsername()
        => Preferences.Default.Get(KeyPCloudUser, string.Empty);

    public void SavePCloudUsername(string? username)
    {
        Preferences.Default.Set(KeyPCloudUser, username ?? string.Empty);
    }

    public string? GetPCloudRootFolder()
        => Preferences.Default.Get(KeyPCloudRoot, string.Empty);

    public void SavePCloudRootFolder(string? rootFolder)
    {
        Preferences.Default.Set(KeyPCloudRoot, rootFolder ?? string.Empty);
    }

    public async Task<string?> GetPCloudTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(KeyPCloudToken);
        }
        catch
        {
            // fallback to preferences if secure storage not available
            return Preferences.Default.Get(KeyPCloudToken, string.Empty);
        }
    }

    public async Task SavePCloudTokenAsync(string? token)
    {
        try
        {
            if (token == null)
            {
                SecureStorage.Default.Remove(KeyPCloudToken);
            }
            else
            {
                await SecureStorage.Default.SetAsync(KeyPCloudToken, token);
            }
        }
        catch
        {
            Preferences.Default.Set(KeyPCloudToken, token ?? string.Empty);
        }
    }
}
