#if ANDROID
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using AndroidX.Core.App;

namespace pCloudPhotoOrganizer.Platforms.Android;

internal static class MediaPermissionRequestHandler
{
    public const int RequestCode = 0x4210;
    private static TaskCompletionSource<bool>? _pendingVideoRequest;

    public static Task<bool> RequestAsync(Activity activity)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            return Task.FromResult(true);

        if (_pendingVideoRequest is not null)
        {
            _pendingVideoRequest.TrySetCanceled();
            _pendingVideoRequest = null;
        }

        var tcs = new TaskCompletionSource<bool>();
        _pendingVideoRequest = tcs;

        ActivityCompat.RequestPermissions(activity, new[] { global::Android.Manifest.Permission.ReadMediaVideo }, RequestCode);
        return tcs.Task;
    }

    public static bool TryHandle(int requestCode, Permission[]? grantResults)
    {
        if (requestCode != RequestCode || _pendingVideoRequest is null)
            return false;

        var granted = grantResults is not null && grantResults.Length > 0 && grantResults[0] == Permission.Granted;
        _pendingVideoRequest.TrySetResult(granted);
        _pendingVideoRequest = null;
        return true;
    }
}
#endif
