using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;

namespace pCloudPhotoOrganizer.Services;
public class PCloudAuthService
{
    private readonly HttpClient _client = new HttpClient { BaseAddress = new Uri("https://api.pcloud.com/") };
    private readonly SettingsService _settings;

    public PCloudAuthService(SettingsService settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Retourne un token existant ou tente un login si absent.
    /// </summary>
    public async Task<string?> GetOrLoginAsync()
    {
        var storedToken = await _settings.GetPCloudTokenAsync();
        if (!string.IsNullOrWhiteSpace(storedToken))
            return storedToken;

        var user = await _settings.GetPCloudUsernameAsync();
        var password = await _settings.GetPCloudPasswordAsync();

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            return null;

        var token = await LoginAsync(user, password);
        if (!string.IsNullOrWhiteSpace(token))
            await _settings.SavePCloudTokenAsync(token);

        return token;
    }

    public async Task<string?> LoginAsync(string username, string password)
    {
        var url = $"login?getauth=1&logout=1&username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";
        var json = await _client.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("result", out var result) && result.GetInt32() == 0)
        {
            return doc.RootElement.GetProperty("auth").GetString();
        }

        return null;
    }

    public Task SaveTokenAsync(string token) => _settings.SavePCloudTokenAsync(token);
}
