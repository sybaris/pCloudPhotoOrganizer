using System.Collections.ObjectModel;
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

        // Charger les dossiers déjà enregistrés (ou ton C:\Temp\PhotosTest)
        foreach (var folder in _settings.GetSelectedFolders())
        {
            Folders.Add(folder);
        }

        FoldersList.ItemsSource = Folders;
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
}
