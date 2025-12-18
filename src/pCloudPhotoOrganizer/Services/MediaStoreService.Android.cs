#if ANDROID
using System.IO;
using Android.Content;
using Android.Util;
using Microsoft.Maui.Controls;
using pCloudPhotoOrganizer.Models;
using pCloudPhotoOrganizer.Platforms.Android;

namespace pCloudPhotoOrganizer.Services;

public class MediaStoreService
{
    public async Task<List<MediaItem>> GetAllMediaAsync()
    {
        var context = Android.App.Application.Context;
        await MediaPermissionHelper.EnsureMediaPermissionAsync();

        var contentResolver = context.ContentResolver;
        if (contentResolver is null)
        {
            Log.Warn("MediaStoreService", "ContentResolver indisponible; aucun media retourne.");
            return new List<MediaItem>();
        }

        var items = new List<MediaItem>();

        foreach (var (contentUri, name, date, size) in MediaStoreQuery.QueryImages(context))
        {
            if (contentUri is null)
            {
                Log.Warn("MediaStoreService", "Content URI null ignoré.");
                continue;
            }

            var uriString = contentUri.ToString();
            if (string.IsNullOrWhiteSpace(uriString))
            {
                Log.Warn("MediaStoreService", "Content URI vide ou invalide ignoré.");
                continue;
            }

            var dt = DateTimeOffset.FromUnixTimeMilliseconds(date).DateTime;
            var systemUri = new Uri(uriString);

            ImageSource? thumbnail = null;
            byte[]? thumbnailBuffer = null;
            Log.Info("MediaStoreService", $"Thumbnail stream attempt {name} uri={uriString} size={size}");

            using var inputStream = contentResolver.OpenInputStream(contentUri);
            if (inputStream is not null)
            {
                using var ms = new MemoryStream();
                inputStream.CopyTo(ms);
                thumbnailBuffer = ms.ToArray();
            }

            if (thumbnailBuffer is not null && thumbnailBuffer.Length > 0)
            {
                thumbnail = ImageSource.FromStream(() => new MemoryStream(thumbnailBuffer));
            }

            if (thumbnail is null)
                throw new Exception($"Thumbnail is null for {name}, URI = {systemUri} ");

            Log.Info("MediaStoreService", $"Thumbnail ready {name}: source={(thumbnail?.GetType().Name ?? "null")}");

            items.Add(new MediaItem
            {
                ContentUri = systemUri,
                FileName = name,
                DateTaken = dt,
                Thumbnail = thumbnail,
                Length = size
            });
        }

        return items;
    }
}
#endif
