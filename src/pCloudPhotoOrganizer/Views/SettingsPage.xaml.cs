using System;
using pCloudPhotoOrganizer.Services;
using pCloudPhotoOrganizer.ViewModels;

namespace pCloudPhotoOrganizer.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsService _settings;
    private bool _isPasswordVisible;
    private ExportMode _exportMode = ExportMode.PCloud;
    private Border? _pCloudGroupBox;
    private Border? _localGroupBox;

    public SettingsViewModel ViewModel { get; }

    public SettingsPage(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        ViewModel = new SettingsViewModel();
        BindingContext = ViewModel;
        UpdatePasswordVisibilityIcon();
        _pCloudGroupBox = this.FindByName<Border>("PCloudGroupBox");
        _localGroupBox = this.FindByName<Border>("LocalGroupBox");

        foreach (var folder in _settings.GetSelectedFolders())
        {
            ViewModel.Folders.Add(folder);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var username = await _settings.GetPCloudUsernameAsync();
        var password = await _settings.GetPCloudPasswordAsync();
        var root = _settings.GetPCloudRootFolder();
        var defaultMoveMode = _settings.GetDefaultMoveMode();
        var exportMode = _settings.GetExportMode();
        var localPath = _settings.GetLocalExportPath();

        PCloudUserEntry.Text = username;
        PCloudPasswordEntry.Text = password;
        PCloudRootEntry.Text = root;
        DefaultMoveSwitch.IsToggled = defaultMoveMode;
        LocalPathEntry.Text = localPath;
        ExportModePCloudRadio.IsChecked = exportMode == ExportMode.PCloud;
        ExportModeLocalRadio.IsChecked = exportMode == ExportMode.Local;

        ApplyExportMode(exportMode);
    }

    private void OnAddFolderClicked(object sender, EventArgs e)
    {
        var path = FolderEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!ViewModel.Folders.Contains(path))
            ViewModel.Folders.Add(path);

        FolderEntry.Text = string.Empty;
    }

    private void OnRemoveFolderClicked(object sender, EventArgs e)
    {
        if (ViewModel.SelectedFolder is string selected)
        {
            ViewModel.Folders.Remove(selected);
            ViewModel.SelectedFolder = null;
        }
    }

    private async void OnSaveAllClicked(object sender, EventArgs e)
    {
        var user = PCloudUserEntry.Text?.Trim() ?? string.Empty;
        var password = PCloudPasswordEntry.Text?.Trim() ?? string.Empty;
        var root = PCloudRootEntry.Text?.Trim() ?? string.Empty;
        var localPath = LocalPathEntry.Text?.Trim() ?? string.Empty;
        var hasPCloudInput = !string.IsNullOrWhiteSpace(user) ||
                             !string.IsNullOrWhiteSpace(password) ||
                             !string.IsNullOrWhiteSpace(root);

        _settings.SaveSelectedFolders(ViewModel.Folders);
        _settings.SaveDefaultMoveMode(DefaultMoveSwitch.IsToggled);
        _settings.SaveExportMode(_exportMode);
        _settings.SaveLocalExportPath(localPath);

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

        await DisplayAlert("Paramêtres", "Les paramêtres ont été sauvegardés.", "OK");
    }

    private void OnExportModeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value)
            return;

        if (sender == ExportModePCloudRadio)
        {
            ApplyExportMode(ExportMode.PCloud);
        }
        else if (sender == ExportModeLocalRadio)
        {
            ApplyExportMode(ExportMode.Local);
        }
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

    private void ApplyExportMode(ExportMode mode)
    {
        _exportMode = mode;
        var isPCloud = mode == ExportMode.PCloud;

        _pCloudGroupBox ??= this.FindByName<Border>("PCloudGroupBox");
        if (_pCloudGroupBox is not null)
        {
            _pCloudGroupBox.IsEnabled = isPCloud;
        }

        PCloudUserEntry.IsEnabled = isPCloud;
        PCloudPasswordEntry.IsEnabled = isPCloud;
        PasswordVisibilityButton.IsEnabled = isPCloud;
        PCloudRootEntry.IsEnabled = isPCloud;

        _localGroupBox ??= this.FindByName<Border>("LocalGroupBox");
        if (_localGroupBox is not null)
        {
            _localGroupBox.IsEnabled = !isPCloud;
        }

        LocalPathEntry.IsEnabled = !isPCloud;
    }
}
