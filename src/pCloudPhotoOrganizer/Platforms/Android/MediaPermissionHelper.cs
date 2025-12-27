#if ANDROID
using System;
using System.Threading.Tasks;
using Android.Util;
using Microsoft.Maui.ApplicationModel;

namespace pCloudPhotoOrganizer.Platforms.Android;

public static class MediaPermissionHelper
{
    private const string LogTag = "MediaPermissionHelper";

    public static Task<bool> EnsureMediaPermissionAsync()
        => EnsureMediaPermissionsAsync();

    public static async Task<bool> EnsureMediaPermissionsAsync()
    {
        try
        {
            var granted = await EnsurePermissionAsync<Permissions.Photos>("READ_MEDIA_IMAGES").ConfigureAwait(false);

            Log.Info(LogTag, $"Media permission granted: {granted}");
            return granted;
        }
        catch (Exception ex)
        {
            Log.Warn(LogTag, $"Media permission request failed: {ex.Message}");
            return false;
        }
    }

    public static Task<bool> EnsureStartupPermissionsAsync(Func<object?>? _) => EnsureStartupPermissionsAsync();

    public static async Task<bool> EnsureStartupPermissionsAsync()
    {
        try
        {
            var mediaGranted = await EnsureMediaPermissionsAsync().ConfigureAwait(false);
            if (!mediaGranted)
            {
                Log.Warn(LogTag, "Startup permissions aborted: media access denied.");
                return false;
            }

            Log.Info(LogTag, "Startup permissions granted.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn(LogTag, $"Startup permissions failed: {ex.Message}");
            return false;
        }
    }

    public static async Task<bool> GetMediaPermissionStatusAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Photos>().ConfigureAwait(false);

            return status == PermissionStatus.Granted;
        }
        catch (Exception ex)
        {
            Log.Warn(LogTag, $"Failed to read media permission status: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> EnsurePermissionAsync<TPermission>(string permissionName)
        where TPermission : Permissions.BasePermission, new()
    {
        var status = await Permissions.CheckStatusAsync<TPermission>().ConfigureAwait(false);
        Log.Info(LogTag, $"{permissionName} permission status (initial): {status}");

        if (status == PermissionStatus.Granted)
            return true;

        status = await Permissions.RequestAsync<TPermission>().ConfigureAwait(false);
        Log.Info(LogTag, $"{permissionName} permission status (after request): {status}");

        return status == PermissionStatus.Granted;
    }
}
#endif
