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
            var persistableTaken = false;

            try
            {
                contentResolver.TakePersistableUriPermission(contentUri, ActivityFlags.GrantReadUriPermission);
                persistableTaken = true;
                Log.Info("MediaStoreService", $"Persistable read permission granted for {contentUri}");
            }
            catch (Exception ex)
            {
                Log.Warn("MediaStoreService", $"Persistable permission FAILED for {contentUri}: {ex.Message}");
            }

            ImageSource? thumbnail = null;
            try
            {
                thumbnail = ImageSource.FromStream(() => contentResolver.OpenInputStream(contentUri) ?? Stream.Null);
            }
            catch
            {
                // Ignore thumbnail errors; upload still possible via content resolver.
            }

            items.Add(new MediaItem
            {
                ContentUri = systemUri,
                FileName = name,
                DateTaken = dt,
                Thumbnail = thumbnail,
                Length = size,
                HasPersistablePermission = persistableTaken
            });
        }

        return items;
    }
}
#endif
