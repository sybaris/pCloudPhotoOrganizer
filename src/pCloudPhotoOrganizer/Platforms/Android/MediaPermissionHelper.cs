#if ANDROID
using System;
using System.Threading.Tasks;
using Android.Content.PM;
using Android.Util;
using AndroidX.Core.Content;
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
            var imagesGranted = await EnsurePermissionAsync<Permissions.Photos>("READ_MEDIA_IMAGES").ConfigureAwait(false);
            var videosGranted = await EnsureVideoPermissionAsync().ConfigureAwait(false);

            Log.Info(LogTag, $"Media permissions granted: images={imagesGranted}, videos={videosGranted}");
            return imagesGranted && videosGranted;
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
            var photoStatus = await Permissions.CheckStatusAsync<Permissions.Photos>().ConfigureAwait(false);
            var videoStatus = await CheckVideoPermissionStatusAsync().ConfigureAwait(false);

            return photoStatus == PermissionStatus.Granted && videoStatus == PermissionStatus.Granted;
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

    private static async Task<bool> EnsureVideoPermissionAsync()
    {
        var status = await CheckVideoPermissionStatusAsync().ConfigureAwait(false);
        if (status == PermissionStatus.Granted)
            return true;

        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            return true;

        var activity = Platform.CurrentActivity;
        if (activity is null)
            return false;

        return await MediaPermissionRequestHandler.RequestAsync(activity).ConfigureAwait(false);
    }

    private static Task<PermissionStatus> CheckVideoPermissionStatusAsync()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            return Task.FromResult(PermissionStatus.Granted);

        var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
        if (context is null)
            return Task.FromResult(PermissionStatus.Unknown);

        var granted = ContextCompat.CheckSelfPermission(context, global::Android.Manifest.Permission.ReadMediaVideo) == Permission.Granted;
        return Task.FromResult(granted ? PermissionStatus.Granted : PermissionStatus.Denied);
    }
}
#endif
