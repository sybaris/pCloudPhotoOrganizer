using System;
using System.Threading.Tasks;
using Microsoft.Maui.Devices;

namespace pCloudPhotoOrganizer.Views;

public partial class PCloudAlbumNamePage : ContentPage
{
    private readonly TaskCompletionSource<string?> _tcs;

    public PCloudAlbumNamePage(string suggestedName, TaskCompletionSource<string?> tcs)
    {
        InitializeComponent();

        _tcs = tcs;
        AlbumNameEntry.Text = suggestedName;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Place le focus et le curseur à la fin du texte saisi
        AlbumNameEntry.Focus();
        var length = AlbumNameEntry.Text?.Length ?? 0;
        AlbumNameEntry.CursorPosition = length;
        AlbumNameEntry.SelectionLength = 0;
    }

    private async void OnOkClicked(object sender, EventArgs e)
    {
        var sanitized = SanitizeName(AlbumNameEntry.Text);
        AlbumNameEntry.Text = sanitized;

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            ShowError("Le nom de l'album ne peut pas être vide.");
            return;
        }

        if (ContainsForbiddenChars(sanitized))
        {
            ShowError("Le nom contient des caractères interdits: / \\ : * ? \" < > |");
            return;
        }

        ClearError();
        TryComplete(sanitized);
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        TryComplete(null);
        if (Navigation?.ModalStack?.Contains(this) == true)
        {
            await Navigation.PopModalAsync();
        }
    }

    protected override bool OnBackButtonPressed()
    {
        TryComplete(null);
        return base.OnBackButtonPressed();
    }

    private string SanitizeName(string? text)
    {
        var name = text ?? string.Empty;
        name = name.TrimEnd();

        // Retire une extension de fichier courante si elle est à la fin
        var lower = name.ToLowerInvariant();
        string[] extensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".heic", ".mp4", ".mov", ".avi", ".mkv" };
        foreach (var ext in extensions)
        {
            if (lower.EndsWith(ext, StringComparison.Ordinal))
            {
                name = name[..^ext.Length];
                break;
            }
        }

        return name;
    }

    private static bool ContainsForbiddenChars(string text)
    {
        const string forbidden = "/\\:*?\"<>|";
        foreach (var c in text)
        {
            if (forbidden.Contains(c))
                return true;
        }

        return false;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;

        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch
            {
                // best-effort haptic
            }
        }
    }

    private void ClearError()
    {
        ErrorLabel.IsVisible = false;
        ErrorLabel.Text = string.Empty;
    }

    private void TryComplete(string? result)
    {
        if (_tcs.Task.IsCompleted)
            return;

        _tcs.TrySetResult(result);
    }
}
