#if ANDROID
using Android.App;
using Android.Content;
using Android.Net;
using Android.Provider;
using Microsoft.Maui.ApplicationModel;
using pCloudPhotoOrganizer.Models;
using pCloudPhotoOrganizer.Platforms.Android;
using Android.Util;
using System.Collections.Generic;
using System.Linq;
using AndroidApp = Android.App.Application;
using AndroidUri = Android.Net.Uri;

namespace pCloudPhotoOrganizer.Services;

public class MediaDeletionService
{
    private const string LogTag = "MediaDeletionService";

    public Task DeleteAsync(MediaItem item) => DeleteAsync(new[] { item });

    public async Task DeleteAsync(IEnumerable<MediaItem> items)
    {
        var context = AndroidApp.Context;
        if (context is null)
        {
            Log.Warn(LogTag, "Android context indisponible; aucune suppression n'est effectuée.");
            return;
        }

        var mediaItems = items?.Where(i => i is not null).ToList() ?? new List<MediaItem>();

        if (mediaItems.Count == 0)
            return;

        await MediaPermissionHelper.EnsureMediaPermissionAsync();

        var requestResult = await TryDeleteWithMediaStoreRequestAsync(context, mediaItems);
        if (requestResult == DeleteRequestResult.Approved)
        {
            Log.Info(LogTag, "Batch delete handled by MediaStore after user approval.");
            return;
        }

        if (requestResult == DeleteRequestResult.Cancelled)
        {
            Log.Warn(LogTag, "User cancelled the MediaStore delete request; no files were deleted.");
            return;
        }

        foreach (var item in mediaItems)
        {
            DeleteDirect(context, item);
        }
    }

    private async Task<DeleteRequestResult> TryDeleteWithMediaStoreRequestAsync(Context context, List<MediaItem> mediaItems)
    {
        var contentResolver = context.ContentResolver;
        if (contentResolver is null)
        {
            Log.Warn(LogTag, "ContentResolver indisponible; suppression directe.");
            return DeleteRequestResult.NotRequested;
        }

        var uris = new List<AndroidUri>();
        foreach (var item in mediaItems)
        {
            var uri = ResolveContentUri(context, item);
            if (uri is not null)
            {
                uris.Add(uri);
            }
            else
            {
                Log.Warn(LogTag, $"Unable to resolve content URI for deletion: {item.DisplayName ?? item.FilePath ?? "<unknown>"}");
            }
        }

        if (uris.Count == 0)
        {
            Log.Warn(LogTag, "No resolvable URIs for batch deletion; skipping MediaStore delete request.");
            return DeleteRequestResult.NotRequested;
        }

        Log.Info(LogTag, $"Creating batch delete request for {uris.Count} item(s).");
        Log.Info(LogTag, $"Delete URIs: {string.Join(", ", uris.Select(u => u.ToString()))}");

        var activity = Platform.CurrentActivity;
        if (activity is null)
        {
            Log.Warn(LogTag, "Cannot launch delete confirmation: CurrentActivity is null. Falling back to direct delete.");
            return DeleteRequestResult.NotRequested;
        }

        try
        {
            var pendingIntent = MediaStore.CreateDeleteRequest(contentResolver, uris);
            var resultTask = DeleteRequestActivityResultHandler.WaitForResultAsync();

            Log.Info(LogTag, "Launching MediaStore delete confirmation intent.");
            activity.StartIntentSenderForResult(
                pendingIntent.IntentSender,
                DeleteRequestActivityResultHandler.RequestCode,
                null,
                0,
                0,
                0);

            var approved = await resultTask.ConfigureAwait(false);
            return approved ? DeleteRequestResult.Approved : DeleteRequestResult.Cancelled;
        }
        catch (Exception ex)
        {
            Log.Warn(LogTag, $"Delete confirmation failed, falling back to direct delete: {ex.Message}");
            DeleteRequestActivityResultHandler.CancelPending();
            return DeleteRequestResult.NotRequested;
        }
    }

    private static void DeleteDirect(Context context, MediaItem item)
    {
        var contentResolver = context.ContentResolver;
        if (contentResolver is null)
        {
            Log.Warn(LogTag, "ContentResolver indisponible; suppression impossible.");
            return;
        }

        if (item.ContentUri is not null)
        {
            var parsed = AndroidUri.Parse(item.ContentUri.ToString());
            if (parsed is null)
            {
                Log.Warn(LogTag, $"URI de contenu invalide pour la suppression : {item.ContentUri}");
                return;
            }

            Log.Info(LogTag, $"Deleting media via ContentResolver: {item.ContentUri}");
            contentResolver.Delete(parsed, null, null);
            return;
        }

        if (!string.IsNullOrWhiteSpace(item.FilePath))
        {
            var collection = MediaStore.Files.GetContentUri("external");
            if (collection is null)
            {
                Log.Warn(LogTag, "Collection MediaStore introuvable pour suppression par chemin.");
                return;
            }

            var selection = $"{MediaStore.IMediaColumns.Data}=?";
            var selectionArgs = new[] { item.FilePath };

            Log.Info(LogTag, $"Deleting media via path lookup: {item.FilePath}");
            contentResolver.Delete(collection, selection, selectionArgs);
        }
    }
    private static AndroidUri? ResolveContentUri(Context context, MediaItem item)
    {
        var contentResolver = context.ContentResolver;
        if (contentResolver is null)
            return null;

        if (item.ContentUri is not null)
            return AndroidUri.Parse(item.ContentUri.ToString());

        if (string.IsNullOrWhiteSpace(item.FilePath))
            return null;

        var collection = MediaStore.Files.GetContentUri("external");
        if (collection is null)
            return null;

        var projection = new[] { Android.Provider.IBaseColumns.Id };
        var selection = $"{MediaStore.IMediaColumns.Data}=?";
        var selectionArgs = new[] { item.FilePath };

        using var cursor = contentResolver.Query(collection, projection, selection, selectionArgs, null);
        if (cursor is not null && cursor.MoveToFirst())
        {
            var idIndex = cursor.GetColumnIndex(Android.Provider.IBaseColumns.Id);
            if (idIndex >= 0)
            {
                var id = cursor.GetLong(idIndex);
                return ContentUris.WithAppendedId(collection, id);
            }
        }

        return null;
    }
}

internal enum DeleteRequestResult
{
    NotRequested,
    Approved,
    Cancelled
}
#endif
