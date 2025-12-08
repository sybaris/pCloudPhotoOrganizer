#if ANDROID
using Android.App;
using Android.Content;
using Android.Net;
using Android.Provider;
using pCloudPhotoOrganizer.Models;
using pCloudPhotoOrganizer.Platforms.Android;
using AndroidApp = Android.App.Application;
using AndroidUri = Android.Net.Uri;

namespace pCloudPhotoOrganizer.Services;

public class MediaDeletionService
{
    public async Task DeleteAsync(MediaItem item)
    {
        var context = AndroidApp.Context;
        await MediaPermissionHelper.EnsureMediaPermissionAsync();

        if (item.ContentUri is not null)
        {
            // Media files must be removed via MediaStore; direct File.Delete is ignored on Android 10+.
            var parsed = AndroidUri.Parse(item.ContentUri.ToString());
            context.ContentResolver.Delete(parsed, null, null);
            return;
        }

        // Fallback: locate the media row by path and ask MediaStore to delete it.
        if (!string.IsNullOrWhiteSpace(item.FilePath))
        {
            var collection = MediaStore.Files.GetContentUri("external");
            var selection = $"{MediaStore.MediaColumns.Data}=?";
            var selectionArgs = new[] { item.FilePath };

            context.ContentResolver.Delete(collection, selection, selectionArgs);
        }
    }
}
#endif
