#if ANDROID
using System;
using System.Threading.Tasks;
using Android.Util;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Application = Microsoft.Maui.Controls.Application;
using static Android.Manifest;

namespace pCloudPhotoOrganizer.Platforms.Android;

public static class MediaPermissionHelper
{
    private const string LogTag = "MediaPermissionHelper";

    public static Task EnsureMediaPermissionAsync(Func<Page?>? pageProvider = null)
        => EnsureMediaPermissionsAsync(pageProvider);

    public static async Task EnsureMediaPermissionsAsync(Func<Page?>? pageProvider = null)
    {
        var status = await Permissions.CheckStatusAsync<MediaReadPermission>();
        Log.Info(LogTag, $"Media read permission status (initial): {status}");
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<MediaReadPermission>();
            Log.Info(LogTag, $"Media read permission status (after request): {status}");
        }

        if (status != PermissionStatus.Granted)
        {
            await ShowDeniedMessageAsync("Photo and video access is required to list and upload your media.", pageProvider);
            throw new UnauthorizedAccessException("Media read permission denied.");
        }

        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            await EnsureLegacyWritePermissionAsync(pageProvider);
        }
    }

    public static async Task EnsureDeletionCapabilityAsync(Func<Page?>? pageProvider = null)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            Log.Info(LogTag, "Android 11+: deletion will use MediaStore confirmation; no extra permission requested.");
            return;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            Log.Info(LogTag, "Android 10: scoped storage deletion via MediaStore; no extra permission requested.");
            return;
        }

        await EnsureLegacyWritePermissionAsync(pageProvider);
    }

    public static Task<PermissionStatus> GetMediaPermissionStatusAsync()
        => Permissions.CheckStatusAsync<MediaReadPermission>();

    public static async Task EnsureStartupPermissionsAsync(Func<Page?>? pageProvider = null)
    {
        try
        {
            await EnsureMediaPermissionsAsync(pageProvider);
            await EnsureDeletionCapabilityAsync(pageProvider);
            Log.Info(LogTag, "Startup permissions completed.");
        }
        catch (Exception ex)
        {
            Log.Warn(LogTag, $"Startup permissions failed: {ex.Message}");
            throw;
        }
    }

    private static async Task EnsureLegacyWritePermissionAsync(Func<Page?>? pageProvider)
    {
        var writeStatus = await Permissions.CheckStatusAsync<MediaWriteLegacyPermission>();
        Log.Info(LogTag, $"Legacy write permission status (initial): {writeStatus}");
        if (writeStatus != PermissionStatus.Granted)
        {
            writeStatus = await Permissions.RequestAsync<MediaWriteLegacyPermission>();
            Log.Info(LogTag, $"Legacy write permission status (after request): {writeStatus}");
        }

        if (writeStatus != PermissionStatus.Granted)
        {
            await ShowDeniedMessageAsync("Storage write permission is required to delete photos on this Android version.", pageProvider);
            throw new UnauthorizedAccessException("Legacy write permission denied.");
        }
    }

    private static Task ShowDeniedMessageAsync(string message, Func<Page?>? pageProvider)
    {
        try
        {
            var page = pageProvider?.Invoke() ?? Application.Current?.MainPage;
            if (page is null)
                return Task.CompletedTask;

            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await page.DisplayAlert("Permissions", message, "OK");
            });
        }
        catch
        {
            return Task.CompletedTask;
        }
    }
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

public class MediaWriteLegacyPermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions
        => new[]
        {
            (Permission.WriteExternalStorage, true)
        };
}
#endif
