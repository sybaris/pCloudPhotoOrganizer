using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
#if ANDROID
using Android.OS;
#endif

namespace pCloudPhotoOrganizer.Views;

public partial class AboutPage : ContentPage
{
    private const string RepoUrl = "https://github.com/sybaris/pCloudPhotoOrganizer";

    public AboutPage()
    {
        InitializeComponent();

        AppNameLabel.Text = AppInfo.Current.Name;
        VersionLabel.Text = $"Version {AppInfo.Current.VersionString}";

        if (DeviceInfo.Current.Platform == DevicePlatform.Android)
        {
            AndroidVersionLabel.Text = $"Version Android {DeviceInfo.Current.VersionString}";
            AndroidVersionLabel.IsVisible = true;

#if ANDROID
            AndroidApiVersionLabel.Text = $"API Android {(int)Build.VERSION.SdkInt}";
            AndroidApiVersionLabel.IsVisible = true;
#endif
        }
    }

    private async void OnLinkTapped(object sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync(RepoUrl);
    }
}
