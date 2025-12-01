using System;
using System.Threading.Tasks;

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
        var text = AlbumNameEntry.Text ?? string.Empty;
        TryComplete(text);
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

    private void TryComplete(string? result)
    {
        if (_tcs.Task.IsCompleted)
            return;

        _tcs.TrySetResult(result);
    }
}
