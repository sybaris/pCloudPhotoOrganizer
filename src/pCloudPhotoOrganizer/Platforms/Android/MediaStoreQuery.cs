using Android.Content;
using Android.Database;
using Android.Net;
using Android.Provider;
using AndroidUri = Android.Net.Uri;

namespace pCloudPhotoOrganizer.Platforms.Android;

public static class MediaStoreQuery
{
    public static IEnumerable<(AndroidUri contentUri, string displayName, long dateTaken, long? size)> QueryImages(Context context)
    {
        var uri = MediaStore.Images.Media.ExternalContentUri;

        string[] projection =
        {
            MediaStore.Images.Media.InterfaceConsts.Id,
            MediaStore.Images.Media.InterfaceConsts.DisplayName,
            MediaStore.Images.Media.InterfaceConsts.DateTaken,
            MediaStore.Images.Media.InterfaceConsts.Size
        };

        using var cursor = context.ContentResolver.Query(uri, projection, null, null, $"{MediaStore.Images.Media.InterfaceConsts.DateTaken} DESC");
        if (cursor == null) yield break;

        int idIndex = cursor.GetColumnIndex(projection[0]);
        int nameIndex = cursor.GetColumnIndex(projection[1]);
        int dateIndex = cursor.GetColumnIndex(projection[2]);
        int sizeIndex = cursor.GetColumnIndex(projection[3]);

        while (cursor.MoveToNext())
        {
            var id = cursor.GetLong(idIndex);
            AndroidUri contentUri = ContentUris.WithAppendedId(uri, id);

            var name = cursor.IsNull(nameIndex) ? $"image_{id}" : cursor.GetString(nameIndex) ?? $"image_{id}";
            var dateTaken = cursor.IsNull(dateIndex) ? 0 : cursor.GetLong(dateIndex);
            long? size = cursor.IsNull(sizeIndex) ? null : cursor.GetLong(sizeIndex);

            yield return (
                contentUri,
                name,
                dateTaken,
                size
            );
        }
    }
}
