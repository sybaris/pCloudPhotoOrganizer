using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace pCloudPhotoOrganizer.Services;

public class SettingsService
{
    private const string KeyFolders = "media_folders";
    private const string KeyPCloudUser = "pcloud_user";
    private const string KeyPCloudRoot = "pcloud_root";
    private const string KeyPCloudToken = "pcloud_token";
    private const string KeyPCloudPassword = "pcloud_password";
    private const string KeyDefaultMoveMode = "default_move_mode";

    private const string ObfuscationPrefix = "obf:";
    private const byte ObfuscationKey = 0x5A;

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

    public string? GetPCloudRootFolder()
        => Preferences.Default.Get(KeyPCloudRoot, string.Empty);

    public void SavePCloudRootFolder(string? rootFolder)
    {
        Preferences.Default.Set(KeyPCloudRoot, rootFolder ?? string.Empty);
    }

    public async Task<string?> GetPCloudUsernameAsync() => await RetrieveSecureAsync(KeyPCloudUser);

    public async Task SavePCloudUsernameAsync(string? username) => await StoreSecureAsync(KeyPCloudUser, username);

    public async Task<string?> GetPCloudPasswordAsync() => await RetrieveSecureAsync(KeyPCloudPassword);

    public async Task SavePCloudPasswordAsync(string? password) => await StoreSecureAsync(KeyPCloudPassword, password);

    public async Task<string?> GetPCloudTokenAsync() => await RetrieveSecureAsync(KeyPCloudToken);

    public async Task SavePCloudTokenAsync(string? token) => await StoreSecureAsync(KeyPCloudToken, token);

    public bool GetDefaultMoveMode() => Preferences.Default.Get(KeyDefaultMoveMode, true);

    public void SaveDefaultMoveMode(bool isMoveMode)
    {
        Preferences.Default.Set(KeyDefaultMoveMode, isMoveMode);
    }

    private async Task StoreSecureAsync(string key, string? value)
    {
        var payload = Obfuscate(value);

        try
        {
            if (string.IsNullOrEmpty(value))
            {
                SecureStorage.Default.Remove(key);
            }
            else
            {
                await SecureStorage.Default.SetAsync(key, payload);
            }

            Preferences.Default.Remove(key);
        }
        catch
        {
            if (string.IsNullOrEmpty(value))
            {
                Preferences.Default.Remove(key);
            }
            else
            {
                Preferences.Default.Set(key, payload);
            }
        }
    }

    private async Task<string?> RetrieveSecureAsync(string key)
    {
        try
        {
            var secured = await SecureStorage.Default.GetAsync(key);
            if (!string.IsNullOrEmpty(secured))
            {
                return Deobfuscate(secured);
            }
        }
        catch
        {
            // ignore, fallback below
        }

        var fromPrefs = Preferences.Default.Get(key, string.Empty);
        return string.IsNullOrWhiteSpace(fromPrefs) ? null : Deobfuscate(fromPrefs);
    }

    private string Obfuscate(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var bytes = Encoding.UTF8.GetBytes(value);
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] ^= ObfuscationKey;
        }

        return ObfuscationPrefix + Convert.ToBase64String(bytes);
    }

    private string Deobfuscate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (!value.StartsWith(ObfuscationPrefix, StringComparison.Ordinal))
            return value;

        try
        {
            var payload = value.Substring(ObfuscationPrefix.Length);
            var bytes = Convert.FromBase64String(payload);

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] ^= ObfuscationKey;
            }

            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}
