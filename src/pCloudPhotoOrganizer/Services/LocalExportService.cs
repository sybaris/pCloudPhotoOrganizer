using System.Diagnostics;
using pCloudPhotoOrganizer.Models;
#if ANDROID
using AndroidApp = Android.App.Application;
using AndroidUri = Android.Net.Uri;
using pCloudPhotoOrganizer.Platforms.Android;
#endif

namespace pCloudPhotoOrganizer.Services;

public class LocalExportService
{
    private readonly SettingsService _settings;
    private readonly MediaDeletionService _deletionService;

    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    public LocalExportService(SettingsService settings, MediaDeletionService deletionService)
    {
        _settings = settings;
        _deletionService = deletionService;
    }

    public async Task CopyOrMoveAsync(MediaItem item, string destinationFolder, bool move)
    {
        ArgumentNullException.ThrowIfNull(item);

        var destinationPath = await CopyFileToDestinationAsync(item, destinationFolder);

        if (move)
            await _deletionService.DeleteAsync(item);
    }

    public static async Task EnsureAllFilesAccessAsync()
    {
#if ANDROID
        await ExternalStoragePermissionHelper.EnsureAllFilesAccessAsync();
#endif
    }

    public string EnsureDestinationFolderExists(string baseFolder, string newFolder)
    {
        var expanded = Environment.ExpandEnvironmentVariables(baseFolder.Trim());
        if (!string.IsNullOrWhiteSpace(newFolder))
            expanded = Path.Combine(expanded, newFolder.Trim());
        var absolute = Path.GetFullPath(expanded);
        Directory.CreateDirectory(absolute);
        return absolute;
    }

    private async Task<string> CopyFileToDestinationAsync(MediaItem item, string destinationFolder)
    {
        var fileName = BuildSafeFileName(item);
        var destinationPath = EnsureUniqueDestination(destinationFolder, fileName);

#if ANDROID
        await using var sourceStream = await OpenReadStreamAsync(item);
        await using var destinationStream = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await sourceStream.CopyToAsync(destinationStream);
#else
        if (string.IsNullOrWhiteSpace(item.FilePath))
            throw new FileNotFoundException($"Chemin du fichier manquant pour '{GetItemLabel(item)}'.");

        if (!File.Exists(item.FilePath))
            throw new FileNotFoundException($"Fichier introuvable : {item.FilePath}");

        File.Copy(item.FilePath, destinationPath);
#endif
        return destinationPath;
    }

    private static string BuildSafeFileName(MediaItem item)
    {
        var candidate = item.FileName;
        if (string.IsNullOrWhiteSpace(candidate) && !string.IsNullOrWhiteSpace(item.FilePath))
            candidate = Path.GetFileName(item.FilePath);

        if (string.IsNullOrWhiteSpace(candidate) && item.ContentUri is not null)
            candidate = item.DisplayName;

        if (string.IsNullOrWhiteSpace(candidate))
            candidate = $"media_{DateTimeOffset.Now:yyyyMMdd_HHmmssfff}.dat";

        var sanitized = new string(candidate.Select(ch => InvalidFileNameChars.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized)
            ? $"media_{DateTimeOffset.Now:yyyyMMdd_HHmmssfff}.dat"
            : sanitized;
    }

    private static string EnsureUniqueDestination(string folder, string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "media";

        var extension = Path.GetExtension(fileName);
        var destination = Path.Combine(folder, baseName + extension);
        var index = 1;

        while (File.Exists(destination))
        {
            destination = Path.Combine(folder, $"{baseName}_{index}{extension}");
            index++;
        }

        return destination;
    }

    private static string GetItemLabel(MediaItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.DisplayName))
            return item.DisplayName!;
        if (!string.IsNullOrWhiteSpace(item.FileName))
            return item.FileName;
        if (!string.IsNullOrWhiteSpace(item.FilePath))
            return Path.GetFileName(item.FilePath);
        return "media";
    }

    private async Task<Stream> OpenReadStreamAsync(MediaItem item)
    {
#if ANDROID
        await MediaPermissionHelper.EnsureMediaPermissionAsync();
        var context = AndroidApp.Context ?? throw new InvalidOperationException("Contexte Android indisponible.");
        var contentResolver = context.ContentResolver ?? throw new InvalidOperationException("ContentResolver indisponible.");

        if (item.ContentUri is not null)
        {
            var androidUri = AndroidUri.Parse(item.ContentUri.ToString());
            if (androidUri is null)
                throw new FileNotFoundException("URI Android invalide pour la copie locale.");

            var stream = contentResolver.OpenInputStream(androidUri);
            if (stream is null)
                throw new FileNotFoundException($"Impossible d'ouvrir '{GetItemLabel(item)}' pour la copie locale.");

            return stream;
        }

        if (!string.IsNullOrWhiteSpace(item.FilePath) && File.Exists(item.FilePath))
            return File.OpenRead(item.FilePath);

        throw new FileNotFoundException($"Impossible de déterminer la source du fichier '{GetItemLabel(item)}'.");
#else
        if (string.IsNullOrWhiteSpace(item.FilePath))
            throw new FileNotFoundException($"Chemin du fichier manquant pour '{GetItemLabel(item)}'.");

        if (!File.Exists(item.FilePath))
            throw new FileNotFoundException($"Fichier introuvable : {item.FilePath}");

        return File.OpenRead(item.FilePath);
#endif
    }
}
