#if !ANDROID
using System.IO;
using pCloudPhotoOrganizer.Models;

namespace pCloudPhotoOrganizer.Services;

public class MediaDeletionService
{
    public Task DeleteAsync(MediaItem item)
    {
        if (File.Exists(item.FilePath))
        {
            File.Delete(item.FilePath);
        }

        return Task.CompletedTask;
    }
}
#endif
