#if FALSE // Android only
using pCloudPhotoOrganizer.Models;
using pCloudPhotoOrganizer.Platforms.Android;

namespace pCloudPhotoOrganizer.Services;
public class MediaStoreService
{
    public async Task<List<MediaItem>> GetAllMediaAsync()
    {
        var context = Android.App.Application.Context;

        var items = new List<MediaItem>();

        foreach (var (path, date) in MediaStoreQuery.QueryImages(context))
        {
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(date).DateTime;

            items.Add(new MediaItem
            {
                FilePath = path,
                DateTaken = dt,
                Thumbnail = ImageSource.FromFile(path)
            });
        }

        return items;
    }
}

#endif
