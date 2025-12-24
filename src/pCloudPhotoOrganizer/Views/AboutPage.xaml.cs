using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

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
        }
    }

    private async void OnLinkTapped(object sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync(RepoUrl);
    }
}
