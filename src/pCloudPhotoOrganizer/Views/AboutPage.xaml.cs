using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using pCloudPhotoOrganizer.Services;
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
            AndroidVersionLabel.Text = $"Version Android {AppInfo.Current.VersionString}";
            AndroidVersionLabel.IsVisible = true;

#if ANDROID
            AndroidApiVersionLabel.Text = $"API Android {(int)Build.VERSION.SdkInt}";
            AndroidApiVersionLabel.IsVisible = true;
#endif
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var buildLabel = this.FindByName<Label>("BuildNumberLabel");
        if (buildLabel is null)
        {
            return;
        }

        try
        {
            var buildNumber = await BuildInfoProvider.GetBuildNumberAsync();
            if (!string.IsNullOrWhiteSpace(buildNumber))
            {
                buildLabel.Text = $"Build {buildNumber}";
                buildLabel.IsVisible = true;
            }
        }
        catch
        {
            buildLabel.IsVisible = false;
        }
    }

    private async void OnLinkTapped(object sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync(RepoUrl);
    }
}
