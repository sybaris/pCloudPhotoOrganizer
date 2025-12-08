using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using pCloudPhotoOrganizer.Platforms.Android;

namespace pCloudPhotoOrganizer;

[Activity(Theme = "@style/Maui.SplashTheme",
          MainLauncher = true,
          ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                                 ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Demande des permissions pour acceder aux photos/videos
        if (!OperatingSystem.IsAndroidVersionAtLeast(23))
            return;

        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            RequestPermissions(new[]
            {
                Android.Manifest.Permission.ReadMediaImages,
                Android.Manifest.Permission.ReadMediaVideo
            }, 0);
        }
        else if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            RequestPermissions(new[]
            {
                Android.Manifest.Permission.ReadExternalStorage
            }, 0);
        }
        else
        {
            RequestPermissions(new[]
            {
                Android.Manifest.Permission.ReadExternalStorage,
                Android.Manifest.Permission.WriteExternalStorage
            }, 0);
        }
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        DeleteRequestActivityResultHandler.TryHandle(requestCode, resultCode);
    }
}
