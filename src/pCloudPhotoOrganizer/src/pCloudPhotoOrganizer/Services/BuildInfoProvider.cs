using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace pCloudPhotoOrganizer.Services;

public static class BuildInfoProvider
{
    private const string BuildInfoFileName = "buildinfo.json";
    private const string DefaultBuildNumber = "local";
    private static string? _cachedBuildNumber;

    public static async Task<string> GetBuildNumberAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedBuildNumber is not null)
        {
            return _cachedBuildNumber;
        }

        try
        {
            using var stream = await FileSystem.Current.OpenAppPackageFileAsync(BuildInfoFileName).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync().ConfigureAwait(false);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("buildNumber", out var buildNumberElement))
            {
                var value = buildNumberElement.GetString();
                _cachedBuildNumber = string.IsNullOrWhiteSpace(value) ? DefaultBuildNumber : value;
            }
            else
            {
                _cachedBuildNumber = DefaultBuildNumber;
            }
        }
        catch
        {
            _cachedBuildNumber ??= DefaultBuildNumber;
        }

        return _cachedBuildNumber!;
    }
}
