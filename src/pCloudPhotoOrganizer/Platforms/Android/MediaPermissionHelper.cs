#if ANDROID
using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using static Android.Manifest;

namespace pCloudPhotoOrganizer.Platforms.Android;

public static class MediaPermissionHelper
{
    public static async Task EnsureMediaPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<MediaReadPermission>();
        if (status == PermissionStatus.Granted)
            return;

        status = await Permissions.RequestAsync<MediaReadPermission>();
        if (status != PermissionStatus.Granted)
            throw new UnauthorizedAccessException("Permission d'accès aux médias refusée.");
    }

    public static Task<PermissionStatus> GetMediaPermissionStatusAsync()
        => Permissions.CheckStatusAsync<MediaReadPermission>();
}

public class MediaReadPermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions
        => OperatingSystem.IsAndroidVersionAtLeast(33)
            ? new[]
            {
                (Permission.ReadMediaImages, true),
                (Permission.ReadMediaVideo, true)
            }
            : new[]
            {
                (Permission.ReadExternalStorage, true)
            };
}
#endif

