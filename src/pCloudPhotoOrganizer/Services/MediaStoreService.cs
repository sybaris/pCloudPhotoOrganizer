#if !ANDROID
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Maui.Controls;
using pCloudPhotoOrganizer.Models;
#if WINDOWS
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;
using Windows.Storage.FileProperties;
#endif

namespace pCloudPhotoOrganizer.Services;

public class MediaStoreService
{
    private readonly SettingsService _settings;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".heic", ".webp"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".avi", ".mkv", ".mpeg", ".mpg", ".wmv"
    };

    public MediaStoreService(SettingsService settings)
    {
        _settings = settings;
    }

    public async Task<List<MediaItem>> GetAllMediaAsync()
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
                var kind = GetMediaKind(path);
                var thumbnail = await LoadThumbnailAsync(path, kind).ConfigureAwait(false);

                if (thumbnail is null && kind == MediaKind.Photo)
                {
                    thumbnail = ImageSource.FromFile(path);
                }

                items.Add(new MediaItem
                {
                    FilePath = path,
                    FileName = fileInfo.Name,
                    DateTaken = fileInfo.LastWriteTime,
                    Thumbnail = thumbnail,
                    Length = fileInfo.Exists ? fileInfo.Length : null,
                    Kind = kind
                });
            }
        }

        return items
            .OrderByDescending(i => i.DateTaken)
            .ToList();
    }

    private static bool IsSupportedMediaFile(string path)
    {
        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext))
            return false;

        return ImageExtensions.Contains(ext) || VideoExtensions.Contains(ext);
    }

    private static MediaKind GetMediaKind(string path)
    {
        var ext = Path.GetExtension(path);
        if (!string.IsNullOrWhiteSpace(ext) && VideoExtensions.Contains(ext))
            return MediaKind.Video;

        return MediaKind.Photo;
    }

    private static Task<ImageSource?> LoadThumbnailAsync(string path, MediaKind kind)
    {
#if WINDOWS
        return LoadWindowsThumbnailAsync(path, kind);
#else
        return Task.FromResult<ImageSource?>(ImageSource.FromFile(path));
#endif
    }

#if WINDOWS
    private static async Task<ImageSource?> LoadWindowsThumbnailAsync(string path, MediaKind kind)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            var mode = kind == MediaKind.Video ? ThumbnailMode.VideosView : ThumbnailMode.PicturesView;
            using var thumbnail = await file.GetThumbnailAsync(mode, 256, ThumbnailOptions.UseCurrentScale);
            if (thumbnail is null || thumbnail.Size == 0)
                return null;

            using var sourceStream = thumbnail.AsStreamForRead();
            using var ms = new MemoryStream();
            await sourceStream.CopyToAsync(ms).ConfigureAwait(false);
            var buffer = ms.ToArray();
            return ImageSource.FromStream(() => new MemoryStream(buffer));
        }
        catch
        {
            return null;
        }
    }
#endif
}
#endif
