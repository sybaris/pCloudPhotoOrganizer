using Android.Content;
using Android.Database;
using Android.Provider;

namespace pCloudPhotoOrganizer.Platforms.Android;

public static class MediaStoreQuery
{
    public static IEnumerable<(string path, long dateTaken)> QueryImages(Context context)
    {
        var uri = MediaStore.Images.Media.ExternalContentUri;

        string[] projection =
        {
            MediaStore.Images.Media.InterfaceConsts.Data,
            MediaStore.Images.Media.InterfaceConsts.DateTaken
        };

        using var cursor = context.ContentResolver.Query(uri, projection, null, null, $"{MediaStore.Images.Media.InterfaceConsts.DateTaken} DESC");
        if (cursor == null) yield break;

        int pathIndex = cursor.GetColumnIndex(projection[0]);
        int dateIndex = cursor.GetColumnIndex(projection[1]);

        while (cursor.MoveToNext())
        {
            yield return (
                cursor.GetString(pathIndex),
                cursor.GetLong(dateIndex)
            );
        }
    }
}
