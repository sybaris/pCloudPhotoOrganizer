using Microsoft.Maui.ApplicationModel;

namespace pCloudPhotoOrganizer.Views;

public partial class AboutPage : ContentPage
{
    private const string RepoUrl = "https://github.com/sybaris/pCloudPhotoOrganizer";

    public AboutPage()
    {
        InitializeComponent();

        AppNameLabel.Text = AppInfo.Current.Name;
        VersionLabel.Text = $"Version {AppInfo.Current.VersionString}";
    }

    private async void OnLinkTapped(object sender, TappedEventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync(RepoUrl);
        }
        catch
        {
            // best effort: no-op if launcher not available
        }
    }
}
