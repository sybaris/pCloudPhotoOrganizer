#if !ANDROID
using System.IO;
using pCloudPhotoOrganizer.Models;
using Microsoft.Maui.Controls;

namespace pCloudPhotoOrganizer.Services;

public class MediaStoreService
{
    private readonly SettingsService _settings;

    public MediaStoreService(SettingsService settings)
    {
        _settings = settings;
    }

    public Task<List<MediaItem>> GetAllMediaAsync()
    {
        var items = new List<MediaItem>();
        var folders = _settings.GetSelectedFolders();

        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder))
                continue;

            var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                                 .Where(IsSupportedMediaFile);

            foreach (var path in files)
            {
                var fileInfo = new FileInfo(path);

                items.Add(new MediaItem
                {
                    FilePath = path,
                    FileName = fileInfo.Name,
                    DateTaken = fileInfo.LastWriteTime,
                    Thumbnail = ImageSource.FromFile(path),
                    Length = fileInfo.Exists ? fileInfo.Length : null
                });
            }
        }

        return Task.FromResult(items);
    }

    private static bool IsSupportedMediaFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();

        return ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif"
                 or ".heic" or ".mp4" or ".mov" or ".avi" or ".mkv";
    }
}
#endif
