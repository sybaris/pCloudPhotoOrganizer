#if !ANDROID
using System.Collections.Generic;
using System.IO;
using pCloudPhotoOrganizer.Models;

namespace pCloudPhotoOrganizer.Services;

public class MediaDeletionService
{
    public Task DeleteAsync(MediaItem item) => DeleteAsync(new[] { item });

    public Task DeleteAsync(IEnumerable<MediaItem> items)
    {
        foreach (var item in items.Where(i => i is not null))
        {
            if (File.Exists(item.FilePath))
            {
                File.Delete(item.FilePath);
            }
        }

        return Task.CompletedTask;
    }
}
#endif
