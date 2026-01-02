using Android.Content;
using Android.Database;
using Android.Net;
using Android.Provider;
using pCloudPhotoOrganizer.Models;
using AndroidUri = Android.Net.Uri;

namespace pCloudPhotoOrganizer.Platforms.Android;

public static class MediaStoreQuery
{
    public static IEnumerable<MediaStoreEntry> QueryMedia(Context context, IEnumerable<string> allowedFolders)
    {
        foreach (var image in QueryImages(context, allowedFolders))
            yield return image;

        foreach (var video in QueryVideos(context, allowedFolders))
            yield return video;
    }

    private static IEnumerable<MediaStoreEntry> QueryImages(Context context, IEnumerable<string> allowedFolders)
    {
        var contentResolver = context.ContentResolver;
        if (contentResolver is null)
            yield break;

        var uri = MediaStore.Images.Media.ExternalContentUri;
        if (uri is null)
            yield break;

        var normalizedFolders = NormalizeFolders(allowedFolders);
        if (!normalizedFolders.Any())
            yield break;

        string[] projection =
        {
            MediaStore.Images.Media.InterfaceConsts.Id,
            MediaStore.Images.Media.InterfaceConsts.DisplayName,
            MediaStore.Images.Media.InterfaceConsts.DateTaken,
            MediaStore.Images.Media.InterfaceConsts.Size,
            MediaStore.Images.Media.InterfaceConsts.Data,
        };

        using var cursor = contentResolver.Query(uri, projection, null, null, $"{MediaStore.Images.Media.InterfaceConsts.DateTaken} DESC");
        if (cursor == null) yield break;

        int idIndex = cursor.GetColumnIndex(projection[0]);
        int nameIndex = cursor.GetColumnIndex(projection[1]);
        int dateIndex = cursor.GetColumnIndex(projection[2]);
        int sizeIndex = cursor.GetColumnIndex(projection[3]);
        int pathIdx = cursor.GetColumnIndex(projection[4]);

        while (cursor.MoveToNext())
        {
            string? path = cursor.GetString(pathIdx);

            if (!IsInAllowedFolders(path, normalizedFolders))
                continue;

            var id = cursor.GetLong(idIndex);
            AndroidUri contentUri = ContentUris.WithAppendedId(uri, id);

            var name = cursor.IsNull(nameIndex) ? $"image_{id}" : cursor.GetString(nameIndex) ?? $"image_{id}";
            var dateTaken = cursor.IsNull(dateIndex) ? 0 : cursor.GetLong(dateIndex);
            long? size = cursor.IsNull(sizeIndex) ? null : cursor.GetLong(sizeIndex);

            yield return new MediaStoreEntry(contentUri, name, dateTaken, size, MediaKind.Photo);
        }
    }

    private static IEnumerable<MediaStoreEntry> QueryVideos(Context context, IEnumerable<string> allowedFolders)
    {
        var contentResolver = context.ContentResolver;
        if (contentResolver is null)
            yield break;

        var uri = MediaStore.Video.Media.ExternalContentUri;
        if (uri is null)
            yield break;

        var normalizedFolders = NormalizeFolders(allowedFolders);
        if (!normalizedFolders.Any())
            yield break;

        string[] projection =
        {
            MediaStore.Video.Media.InterfaceConsts.Id,
            MediaStore.Video.Media.InterfaceConsts.DisplayName,
            MediaStore.Video.Media.InterfaceConsts.DateTaken,
            MediaStore.Video.Media.InterfaceConsts.Size,
            MediaStore.Video.Media.InterfaceConsts.Data,
        };

        using var cursor = contentResolver.Query(uri, projection, null, null, $"{MediaStore.Video.Media.InterfaceConsts.DateTaken} DESC");
        if (cursor == null) yield break;

        int idIndex = cursor.GetColumnIndex(projection[0]);
        int nameIndex = cursor.GetColumnIndex(projection[1]);
        int dateIndex = cursor.GetColumnIndex(projection[2]);
        int sizeIndex = cursor.GetColumnIndex(projection[3]);
        int pathIdx = cursor.GetColumnIndex(projection[4]);

        while (cursor.MoveToNext())
        {
            string? path = cursor.GetString(pathIdx);

            if (!IsInAllowedFolders(path, normalizedFolders))
                continue;

            var id = cursor.GetLong(idIndex);
            AndroidUri contentUri = ContentUris.WithAppendedId(uri, id);

            var name = cursor.IsNull(nameIndex) ? $"video_{id}" : cursor.GetString(nameIndex) ?? $"video_{id}";
            var dateTaken = cursor.IsNull(dateIndex) ? 0 : cursor.GetLong(dateIndex);
            long? size = cursor.IsNull(sizeIndex) ? null : cursor.GetLong(sizeIndex);

            yield return new MediaStoreEntry(contentUri, name, dateTaken, size, MediaKind.Video);
        }
    }

    private static List<string> NormalizeFolders(IEnumerable<string> allowedFolders)
        => allowedFolders
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f.Replace("\\", "/").ToLowerInvariant())
            .ToList();

    private static bool IsInAllowedFolders(string? path, List<string> normalizedFolders)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        path = path.Replace("\\", "/").ToLowerInvariant();

        // Vérifier si le chemin commence par l'un des dossiers autorisés
        return normalizedFolders.Any(folder => path.StartsWith(folder));
    }
}

public readonly record struct MediaStoreEntry(AndroidUri ContentUri, string DisplayName, long DateTaken, long? Size, MediaKind Kind);
