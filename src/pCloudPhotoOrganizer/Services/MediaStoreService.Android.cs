#if ANDROID
using System.IO;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Microsoft.Maui.Controls;
using pCloudPhotoOrganizer.Models;
using pCloudPhotoOrganizer.Platforms.Android;
using AndroidUri = Android.Net.Uri;

namespace pCloudPhotoOrganizer.Services;

public class MediaStoreService
{
    private const string LogTag = "MediaStoreService";
    private readonly SettingsService _settingsService;

    public MediaStoreService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<List<MediaItem>> GetAllMediaAsync()
    {
        var context = Android.App.Application.Context;
        await MediaPermissionHelper.EnsureMediaPermissionAsync();

        var contentResolver = context.ContentResolver;
        if (contentResolver is null)
        {
            Log.Warn(LogTag, "ContentResolver indisponible; aucun media retourne.");
            return new List<MediaItem>();
        }

        var allowedFolders = _settingsService.GetSelectedFolders();
        if (!allowedFolders.Any())
        {
            Log.Warn(LogTag, "Aucun dossier configuré dans les paramètres.");
            return new List<MediaItem>();
        }

        var entries = MediaStoreQuery.QueryMedia(context, allowedFolders).ToList();
        var items = new List<MediaItem>(entries.Count);

        foreach (var entry in entries)
        {
            var androidUri = entry.ContentUri;
            if (androidUri is null)
            {
                Log.Warn(LogTag, "Content URI null ignoré.");
                continue;
            }

            var uriString = androidUri.ToString();
            if (string.IsNullOrWhiteSpace(uriString))
            {
                Log.Warn(LogTag, "Content URI vide ou invalide ignoré.");
                continue;
            }

            var dateTaken = entry.DateTaken > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(entry.DateTaken).DateTime
                : DateTime.Now;

            var thumbnail = TryCreateThumbnail(contentResolver, androidUri, entry.Kind == MediaKind.Video);
            if (thumbnail is null)
            {
                Log.Warn(LogTag, $"Impossible de générer une miniature pour '{entry.DisplayName}'.");
            }

            items.Add(new MediaItem
            {
                ContentUri = new Uri(uriString),
                FileName = entry.DisplayName,
                DateTaken = dateTaken,
                Thumbnail = thumbnail,
                Length = entry.Size,
                Kind = entry.Kind
            });
        }

        return items
            .OrderByDescending(i => i.DateTaken)
            .ToList();
    }

    private static ImageSource? TryCreateThumbnail(ContentResolver contentResolver, AndroidUri uri, bool isVideo)
    {
        try
        {
            using var bitmap = contentResolver.LoadThumbnail(uri, new global::Android.Util.Size(512, 512), null);
            var imageSource = CreateImageSource(bitmap);
            if (imageSource is not null)
                return imageSource;
        }
        catch (Exception ex)
        {
            Log.Warn(LogTag, $"Echec LoadThumbnail pour {uri}: {ex.Message}");
        }

        if (!isVideo)
        {
            try
            {
                using var inputStream = contentResolver.OpenInputStream(uri);
                if (inputStream is not null)
                {
                    using var ms = new MemoryStream();
                    inputStream.CopyTo(ms);
                    var buffer = ms.ToArray();
                    if (buffer.Length > 0)
                        return ImageSource.FromStream(() => new MemoryStream(buffer));
                }
            }
            catch (Exception ex)
            {
                Log.Warn(LogTag, $"Echec de lecture du flux pour miniature {uri}: {ex.Message}");
            }
        }

        return null;
    }

    private static ImageSource? CreateImageSource(Bitmap? bitmap)
    {
        if (bitmap is null)
            return null;

        using var ms = new MemoryStream();
        bitmap.Compress(Bitmap.CompressFormat.Png, 90, ms);
        var data = ms.ToArray();
        return data.Length == 0 ? null : ImageSource.FromStream(() => new MemoryStream(data));
    }
}
#endif
