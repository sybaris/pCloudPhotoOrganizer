#if ANDROID
using System.Threading.Tasks;
using Android.App;

namespace pCloudPhotoOrganizer.Platforms.Android;

internal static class DeleteRequestActivityResultHandler
{
    public const int RequestCode = 9001;

    private static TaskCompletionSource<bool>? _pendingResult;

    public static Task<bool> WaitForResultAsync()
    {
        _pendingResult?.TrySetCanceled();
        _pendingResult = new TaskCompletionSource<bool>();
        return _pendingResult.Task;
    }

    public static bool TryHandle(int requestCode, Result resultCode)
    {
        if (requestCode != RequestCode || _pendingResult is null)
            return false;

        var approved = resultCode == Result.Ok;
        _pendingResult.TrySetResult(approved);
        _pendingResult = null;
        return true;
    }

    public static void CancelPending()
    {
        _pendingResult?.TrySetCanceled();
        _pendingResult = null;
    }
}
#endif
