using System.Collections.ObjectModel;
using System.Threading.Tasks;
using pCloudPhotoOrganizer.Services;

namespace pCloudPhotoOrganizer.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsService _settings;

    public ObservableCollection<string> Folders { get; } = new();

    public SettingsPage(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;

        foreach (var folder in _settings.GetSelectedFolders())
        {
            Folders.Add(folder);
        }

        FoldersList.ItemsSource = Folders;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var username = await _settings.GetPCloudUsernameAsync();
        var password = await _settings.GetPCloudPasswordAsync();
        var root = _settings.GetPCloudRootFolder();

        PCloudUserEntry.Text = username;
        PCloudPasswordEntry.Text = password;
        PCloudRootEntry.Text = root;
    }

    private void OnAddFolderClicked(object sender, EventArgs e)
    {
        var path = FolderEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!Folders.Contains(path))
            Folders.Add(path);

        FolderEntry.Text = string.Empty;
    }

    private void OnRemoveFolderClicked(object sender, EventArgs e)
    {
        if (FoldersList.SelectedItem is string selected)
        {
            Folders.Remove(selected);
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        _settings.SaveSelectedFolders(Folders);
        await DisplayAlert("Paramètres", "Les dossiers ont été sauvegardés.", "OK");
    }

    private async void OnSavePCloudClicked(object sender, EventArgs e)
    {
        var user = PCloudUserEntry.Text?.Trim() ?? string.Empty;
        var password = PCloudPasswordEntry.Text?.Trim() ?? string.Empty;
        var root = PCloudRootEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(user))
        {
            ShowPCloudError("L'identifiant pCloud est requis.");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowPCloudError("Le mot de passe pCloud est requis.");
            return;
        }

        ClearPCloudError();

        await _settings.SavePCloudUsernameAsync(user);
        _settings.SavePCloudRootFolder(root);
        await _settings.SavePCloudPasswordAsync(password);

        await DisplayAlert("pCloud", "Identifiants pCloud sauvegardés.", "OK");
    }

    private void ShowPCloudError(string message)
    {
        PCloudErrorLabel.Text = message;
        PCloudErrorLabel.IsVisible = true;
    }

    private void ClearPCloudError()
    {
        PCloudErrorLabel.IsVisible = false;
        PCloudErrorLabel.Text = string.Empty;
    }
}
