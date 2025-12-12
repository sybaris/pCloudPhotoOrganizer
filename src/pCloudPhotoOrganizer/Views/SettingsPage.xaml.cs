using System;
using System.Collections.ObjectModel;
using pCloudPhotoOrganizer.Services;

namespace pCloudPhotoOrganizer.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsService _settings;
    private bool _isPasswordVisible;

    public ObservableCollection<string> Folders { get; } = new();

    public SettingsPage(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        UpdatePasswordVisibilityIcon();

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
        var defaultMoveMode = _settings.GetDefaultMoveMode();

        PCloudUserEntry.Text = username;
        PCloudPasswordEntry.Text = password;
        PCloudRootEntry.Text = root;
        DefaultMoveSwitch.IsToggled = defaultMoveMode;
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

    private async void OnSaveAllClicked(object sender, EventArgs e)
    {
        var user = PCloudUserEntry.Text?.Trim() ?? string.Empty;
        var password = PCloudPasswordEntry.Text?.Trim() ?? string.Empty;
        var root = PCloudRootEntry.Text?.Trim() ?? string.Empty;
        var hasPCloudInput = !string.IsNullOrWhiteSpace(user) ||
                             !string.IsNullOrWhiteSpace(password) ||
                             !string.IsNullOrWhiteSpace(root);

        _settings.SaveSelectedFolders(Folders);
        _settings.SaveDefaultMoveMode(DefaultMoveSwitch.IsToggled);

        if (hasPCloudInput && string.IsNullOrWhiteSpace(user))
        {
            ShowPCloudError("L'identifiant pCloud est requis.");
            await DisplayAlert("pCloud", "L'identifiant pCloud est requis.", "OK");
            return;
        }

        if (hasPCloudInput && string.IsNullOrWhiteSpace(password))
        {
            ShowPCloudError("Le mot de passe pCloud est requis.");
            await DisplayAlert("pCloud", "Le mot de passe pCloud est requis.", "OK");
            return;
        }

        ClearPCloudError();

        if (hasPCloudInput)
        {
            await _settings.SavePCloudUsernameAsync(user);
            _settings.SavePCloudRootFolder(root);
            await _settings.SavePCloudPasswordAsync(password);
        }
        else
        {
            await _settings.SavePCloudUsernameAsync(string.Empty);
            _settings.SavePCloudRootFolder(string.Empty);
            await _settings.SavePCloudPasswordAsync(string.Empty);
        }

        await DisplayAlert("ParamŠtres", "Les paramŠtres ont ‚t‚ sauvegard‚s.", "OK");
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

    private void OnTogglePasswordVisibilityClicked(object sender, EventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        PCloudPasswordEntry.IsPassword = !_isPasswordVisible;
        UpdatePasswordVisibilityIcon();
    }

    private void UpdatePasswordVisibilityIcon()
    {
        var source = _isPasswordVisible ? "icon_eye_open.svg" : "icon_eye_closed.svg";
        PasswordVisibilityButton.Source = source;
        SemanticProperties.SetDescription(PasswordVisibilityButton, _isPasswordVisible ? "Masquer le mot de passe" : "Afficher le mot de passe");
    }
}
