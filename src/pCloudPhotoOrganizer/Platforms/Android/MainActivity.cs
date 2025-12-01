using Android.App;
using Android.Content.PM;
using Android.OS;

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

        // Demande des permissions pour accéder aux photos/vidéos
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            RequestPermissions(new[]
            {
                Android.Manifest.Permission.ReadMediaImages,
                Android.Manifest.Permission.ReadMediaVideo
            }, 0);
        }
        else
        {
            RequestPermissions(new[]
            {
                Android.Manifest.Permission.ReadExternalStorage
            }, 0);
        }
    }
}
